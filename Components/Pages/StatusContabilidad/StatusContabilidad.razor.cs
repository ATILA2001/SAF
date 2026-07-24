using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.Application.StatusContabilidad.Dtos;
using SAF.Shared;

namespace SAF.Components.Pages.StatusContabilidad;

public partial class StatusContabilidad
{
    [Inject] private IStatusContabilidadService StatusContabilidadService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] private INotificationHelper Notification { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private RadzenDataGrid<StatusContabilidadViewModel> _grid = null!;
    private List<StatusContabilidadViewModel> _items = new();
    private bool _loading = true;

    // Marcas de la col. "Fecha de Ingreso Factura (correcta)" del Excel cuando no hay fecha.
    private static readonly string[] _sinFacturaMotivos = ["N/C", "CCOO", "PAV", "Anulado"];

    private IReadOnlyList<StatusContableOpcion> _statusContableOpciones = Array.Empty<StatusContableOpcion>();
    private IReadOnlyList<TramitadorCuentasPagarOpcion> _tramitadoresCuentasPagar = Array.Empty<TramitadorCuentasPagarOpcion>();
    private IReadOnlyList<TramitadorLiquidacionesOpcion> _tramitadoresLiquidaciones = Array.Empty<TramitadorLiquidacionesOpcion>();

    protected override async Task OnInitializedAsync()
    {
        await LoadPermissionsAsync("/status-contabilidad");

        // Secuencial: comparten el AppDbContext scoped (EF Core no admite
        // operaciones concurrentes sobre la misma instancia de DbContext).
        _statusContableOpciones    = await LookupService.GetStatusContableOpcionesAsync();
        _tramitadoresCuentasPagar  = await LookupService.GetTramitadoresCuentasPagarAsync();
        _tramitadoresLiquidaciones = await LookupService.GetTramitadoresLiquidacionesAsync();
        _items = (await StatusContabilidadService.GetAllAsync()).ToList();
        _loading = false;
    }

    private async Task OnRowUpdate(StatusContabilidadViewModel item)
    {
        // La UI esconde el botón, pero el permiso se revalida acá (server-side).
        if (!CanEdit)
        {
            Notification.ShowError("No tenés permiso para editar Status Contabilidad.", "Permiso denegado");
            return;
        }

        item.StatusContableNombre = _statusContableOpciones.FirstOrDefault(x => x.Id == item.StatusContableOpcionId)?.Nombre;
        item.TramitadorCuentasPagarNombre = _tramitadoresCuentasPagar.FirstOrDefault(x => x.Id == item.TramitadorCuentasPagarOpcionId)?.Nombre;
        item.TramitadorLiquidacionesNombre = _tramitadoresLiquidaciones.FirstOrDefault(x => x.Id == item.TramitadorLiquidacionesOpcionId)?.Nombre;

        await StatusContabilidadService.UpsertAsync(item);
        await _grid.Reload();
    }

    private Task OnRowCreate(StatusContabilidadViewModel item) => Task.CompletedTask;

    private async Task ExportarExcel()
    {
        var bytes = ExportService.ExportToXlsx(_items, "Status Contabilidad");
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBase64", base64, "StatusContabilidad.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
