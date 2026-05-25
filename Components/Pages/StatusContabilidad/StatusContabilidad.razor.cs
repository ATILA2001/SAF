using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.ViewModels.StatusContabilidad;

namespace SAF.Components.Pages.StatusContabilidad;

public partial class StatusContabilidad
{
    [Inject] private IStatusContabilidadService StatusContabilidadService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private RadzenDataGrid<StatusContabilidadViewModel> _grid = null!;
    private List<StatusContabilidadViewModel> _items = new();
    private bool _loading = true;

    private IReadOnlyList<StatusContableOpcion> _statusContableOpciones = Array.Empty<StatusContableOpcion>();
    private IReadOnlyList<TramitadorCuentasPagarOpcion> _tramitadoresCuentasPagar = Array.Empty<TramitadorCuentasPagarOpcion>();
    private IReadOnlyList<TramitadorLiquidacionesOpcion> _tramitadoresLiquidaciones = Array.Empty<TramitadorLiquidacionesOpcion>();

    protected override async Task OnInitializedAsync()
    {
        await LoadPermissionsAsync("/status-contabilidad");

        var itemsTask  = StatusContabilidadService.GetAllAsync();
        var scTask     = LookupService.GetStatusContableOpcionesAsync();
        var cpTask     = LookupService.GetTramitadoresCuentasPagarAsync();
        var liqTask    = LookupService.GetTramitadoresLiquidacionesAsync();

        await Task.WhenAll(itemsTask, scTask, cpTask, liqTask);

        _statusContableOpciones    = await scTask;
        _tramitadoresCuentasPagar  = await cpTask;
        _tramitadoresLiquidaciones = await liqTask;
        _items = (await itemsTask).ToList();
        _loading = false;
    }

    private async Task OnRowUpdate(StatusContabilidadViewModel item)
    {
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
