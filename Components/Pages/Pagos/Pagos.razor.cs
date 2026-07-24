using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.Application.Pagos.Dtos;
using SAF.Shared;

namespace SAF.Components.Pages.Pagos;

public partial class Pagos
{
    [Inject] private IPagosService PagosService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] private IDevengadoSyncService SyncService { get; set; } = null!;
    [Inject] private INotificationHelper Notification { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private RadzenDataGrid<PagoViewModel> _grid = null!;
    private List<PagoViewModel> _items = new();
    private bool _loading = true;
    private bool _syncing;
    private DateTime? _ultimaActualizacion;

    private IReadOnlyList<StatusDgayfOpcion> _statusDgayfOpciones = Array.Empty<StatusDgayfOpcion>();
    private IReadOnlyList<StatusOpOpcion> _statusOpOpciones = Array.Empty<StatusOpOpcion>();

    protected override async Task OnInitializedAsync()
    {
        await LoadPermissionsAsync("/pagos");

        try
        {
            // Secuencial: comparten el AppDbContext scoped (EF Core no admite
            // operaciones concurrentes sobre la misma instancia de DbContext).
            _statusDgayfOpciones = await LookupService.GetStatusDgayfOpcionesAsync();
            _statusOpOpciones = await LookupService.GetStatusOpOpcionesAsync();
            _items = (await PagosService.GetAllAsync()).ToList();
            _ultimaActualizacion = await PagosService.GetUltimaFechaImputacionAsync();
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al cargar Pagos");
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnRowUpdate(PagoViewModel item)
    {
        // La UI esconde el botón, pero el permiso se revalida acá (server-side).
        if (!CanEdit)
        {
            Notification.ShowError("No tenés permiso para editar Pagos.", "Permiso denegado");
            return;
        }

        // Actualizar nombre visible en la grilla
        item.StatusDgayfNombre = _statusDgayfOpciones.FirstOrDefault(x => x.Id == item.StatusDgayfOpcionId)?.Nombre;
        item.StatusOpNombre = _statusOpOpciones.FirstOrDefault(x => x.Id == item.StatusOpOpcionId)?.Nombre;

        try
        {
            await PagosService.UpsertAsync(item);
            await _grid.Reload();
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al guardar");
        }
    }

    private async Task AddRow()
    {
        var nuevo = new PagoViewModel { FechaDevengado = DateTime.Today };
        await _grid.InsertRow(nuevo);
    }

    private async Task OnRowCreate(PagoViewModel item)
    {
        // La UI esconde el botón, pero el permiso se revalida acá (server-side).
        if (!CanCreate)
        {
            Notification.ShowError("No tenés permiso para cargar devengados.", "Permiso denegado");
            return;
        }

        try
        {
            await PagosService.CreateDevengadoAsync(item);
            _items = (await PagosService.GetAllAsync()).ToList();
            await _grid.Reload();
            Notification.ShowSuccess($"Devengado {item.TipoDev} {item.NroDev} cargado.", "Alta exitosa");
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al cargar devengado");
        }
    }

    private async Task DeleteRow(PagoViewModel item)
    {
        if (!CanDelete)
        {
            Notification.ShowError("No tenés permiso para eliminar devengados.", "Permiso denegado");
            return;
        }

        var confirmado = await DialogService.Confirm(
            $"Se eliminará la fila del devengado {item.TipoDev} {item.NroDev} " +
            $"(expediente {item.Expediente}, importe {item.Importe:N2}) junto con sus datos " +
            "cargados (status, observaciones, fechas). Esta acción no se puede deshacer.",
            "Eliminar devengado",
            new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar" });
        if (confirmado != true) return;

        try
        {
            await PagosService.DeleteDevengadoAsync(item.Id);
            _items = (await PagosService.GetAllAsync()).ToList();
            await _grid.Reload();
            Notification.ShowSuccess($"Devengado {item.TipoDev} {item.NroDev} eliminado.", "Baja exitosa");
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al eliminar");
        }
    }

    private async Task SincronizarDevengados()
    {
        if (!CanCreate)
        {
            Notification.ShowError("No tenés permiso para sincronizar devengados.", "Permiso denegado");
            return;
        }

        _syncing = true;
        try
        {
            var result = await SyncService.SyncAsync();
            _ultimaActualizacion = result.UltimaFecha ?? _ultimaActualizacion;

            switch (result.Status)
            {
                case SyncStatus.Importado:
                    _items = (await PagosService.GetAllAsync()).ToList();
                    Notification.ShowSuccess(
                        $"{result.Insertados} devengado(s) nuevo(s) importado(s).",
                        "Sincronización completa");
                    break;

                case SyncStatus.YaActualizado:
                    Notification.ShowInfo(
                        $"Los datos ya están actualizados al {result.UltimaFecha:dd/MM/yyyy}. No hay filas nuevas para importar.",
                        "Datos actualizados");
                    break;

                case SyncStatus.SinDatosEnIvc:
                    Notification.ShowWarning(
                        "No hay devengados disponibles en IVC para importar.",
                        "Sin datos");
                    break;
            }
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al sincronizar");
        }
        finally
        {
            _syncing = false;
        }
    }

    private async Task ExportarExcel()
    {
        var bytes = ExportService.ExportToXlsx(_items, "Pagos");
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBase64", base64, "Pagos.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
