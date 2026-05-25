using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.ViewModels.Pagos;

namespace SAF.Components.Pages.Pagos;

public partial class Pagos
{
    [Inject] private IPagosService PagosService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private RadzenDataGrid<PagoViewModel> _grid = null!;
    private List<PagoViewModel> _items = new();
    private bool _loading = true;

    private IReadOnlyList<StatusDgayfOpcion> _statusDgayfOpciones = Array.Empty<StatusDgayfOpcion>();
    private IReadOnlyList<StatusOpOpcion> _statusOpOpciones = Array.Empty<StatusOpOpcion>();

    protected override async Task OnInitializedAsync()
    {
        await LoadPermissionsAsync("/pagos");

        var itemsTask = PagosService.GetAllAsync();
        var dgayfTask = LookupService.GetStatusDgayfOpcionesAsync();
        var opTask    = LookupService.GetStatusOpOpcionesAsync();

        await Task.WhenAll(itemsTask, dgayfTask, opTask);

        _statusDgayfOpciones = await dgayfTask;
        _statusOpOpciones = await opTask;
        _items = (await itemsTask).ToList();
        _loading = false;
    }

    private async Task OnRowUpdate(PagoViewModel item)
    {
        // Actualizar nombre visible en la grilla
        item.StatusDgayfNombre = _statusDgayfOpciones.FirstOrDefault(x => x.Id == item.StatusDgayfOpcionId)?.Nombre;
        item.StatusOpNombre = _statusOpOpciones.FirstOrDefault(x => x.Id == item.StatusOpOpcionId)?.Nombre;

        await PagosService.UpsertAsync(item);
        await _grid.Reload();
    }

    private Task OnRowCreate(PagoViewModel item) => Task.CompletedTask;

    private async Task ExportarExcel()
    {
        var bytes = ExportService.ExportToXlsx(_items, "Pagos");
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBase64", base64, "Pagos.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
