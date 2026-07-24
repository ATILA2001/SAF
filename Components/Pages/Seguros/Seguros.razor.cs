using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.Application.Seguros.Dtos;
using SAF.Shared;

namespace SAF.Components.Pages.Seguros;

public partial class Seguros
{
    [Inject] private ISeguroService SeguroService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] private INotificationHelper Notification { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private RadzenDataGrid<SeguroViewModel> _grid = null!;
    private List<SeguroViewModel> _items = new();
    private bool _loading = true;

    private IReadOnlyList<SeguroOpcion> _seguroOpciones = Array.Empty<SeguroOpcion>();

    protected override async Task OnInitializedAsync()
    {
        await LoadPermissionsAsync("/seguros");
        // Secuencial: comparten el AppDbContext scoped.
        _seguroOpciones = await LookupService.GetSeguroOpcionesAsync();
        _items = (await SeguroService.GetAllAsync()).ToList();
        _loading = false;
    }

    private async Task AddRow()
    {
        var nuevo = new SeguroViewModel();
        await _grid.InsertRow(nuevo);
    }

    private async Task OnRowCreate(SeguroViewModel item)
    {
        // La UI esconde los botones, pero el permiso se revalida acá (server-side).
        if (!CanCreate)
        {
            Notification.ShowError("No tenés permiso para crear seguros.", "Permiso denegado");
            return;
        }

        try
        {
            await SeguroService.CreateAsync(item);
            await ReloadAsync();
            Notification.ShowSuccess("Seguro creado.", "Alta exitosa");
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al crear");
        }
    }

    private async Task OnRowUpdate(SeguroViewModel item)
    {
        if (!CanEdit)
        {
            Notification.ShowError("No tenés permiso para editar seguros.", "Permiso denegado");
            return;
        }

        try
        {
            await SeguroService.UpdateAsync(item);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al guardar");
        }
    }

    private void CancelEdit(SeguroViewModel item) => _grid.CancelEditRow(item);

    private async Task DeleteRow(SeguroViewModel item)
    {
        if (!CanDelete)
        {
            Notification.ShowError("No tenés permiso para eliminar seguros.", "Permiso denegado");
            return;
        }

        try
        {
            if (item.Id != 0)
                await SeguroService.DeleteAsync(item.Id);
            await ReloadAsync();
            Notification.ShowSuccess("Seguro eliminado.", "Baja exitosa");
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al eliminar");
        }
    }

    private async Task ReloadAsync()
    {
        _items = (await SeguroService.GetAllAsync()).ToList();
        await _grid.Reload();
    }

    private async Task ExportarExcel()
    {
        var bytes = ExportService.ExportToXlsx(_items, "Seguros");
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBase64", base64, "Seguros.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
