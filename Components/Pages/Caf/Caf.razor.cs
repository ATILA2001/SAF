using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor;
using SAF.Services.Abstractions;
using SAF.Application.Caf.Dtos;
using SAF.Shared;

namespace SAF.Components.Pages.Caf;

public partial class Caf
{
    [Inject] private ICafService CafService { get; set; } = null!;
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] private INotificationHelper Notification { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private RadzenDataGrid<CafViewModel> _grid = null!;
    private List<CafViewModel> _items = new();
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadPermissionsAsync("/caf");
        _items = (await CafService.GetAllAsync()).ToList();
        _loading = false;
    }

    private async Task AddRow()
    {
        var nuevo = new CafViewModel { Anio = DateTime.Now.Year };
        await _grid.InsertRow(nuevo);
    }

    private async Task OnRowCreate(CafViewModel item)
    {
        // La UI esconde los botones, pero el permiso se revalida acá (server-side).
        if (!CanCreate)
        {
            Notification.ShowError("No tenés permiso para crear expedientes CAF.", "Permiso denegado");
            return;
        }

        try
        {
            var creado = await CafService.CreateAsync(item);
            await ReloadAsync();
            Notification.ShowSuccess("Expediente CAF creado.", "Alta exitosa");
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al crear");
        }
    }

    private async Task OnRowUpdate(CafViewModel item)
    {
        if (!CanEdit)
        {
            Notification.ShowError("No tenés permiso para editar expedientes CAF.", "Permiso denegado");
            return;
        }

        try
        {
            await CafService.UpdateAsync(item);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al guardar");
        }
    }

    private void CancelEdit(CafViewModel item) => _grid.CancelEditRow(item);

    private async Task DeleteRow(CafViewModel item)
    {
        if (!CanDelete)
        {
            Notification.ShowError("No tenés permiso para eliminar expedientes CAF.", "Permiso denegado");
            return;
        }

        try
        {
            if (item.Id != 0)
                await CafService.DeleteAsync(item.Id);
            await ReloadAsync();
            Notification.ShowSuccess("Expediente CAF eliminado.", "Baja exitosa");
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al eliminar");
        }
    }

    private async Task ReloadAsync()
    {
        _items = (await CafService.GetAllAsync()).ToList();
        await _grid.Reload();
    }

    private async Task ExportarExcel()
    {
        var bytes = ExportService.ExportToXlsx(_items, "Expedientes CAF");
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBase64", base64, "ExpedientesCaf.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
