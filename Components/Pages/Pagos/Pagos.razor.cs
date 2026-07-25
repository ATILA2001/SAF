using Microsoft.AspNetCore.Components;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.Application.Pagos;
using SAF.Application.Pagos.Dtos;

namespace SAF.Components.Pages.Pagos;

public partial class Pagos
{
    [Inject] private IPagosService PagosService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;
    [Inject] private IDevengadoSyncService SyncService { get; set; } = null!;

    private bool _syncing;
    private DateTime? _ultimaActualizacion;

    private IReadOnlyList<StatusDgayfOpcion> _statusDgayfOpciones = Array.Empty<StatusDgayfOpcion>();
    private IReadOnlyList<StatusOpOpcion> _statusOpOpciones = Array.Empty<StatusOpOpcion>();

    protected override string PageUrl => "/pagos";
    protected override string TituloEntidad => "devengados";
    protected override string ExportNombreHoja => "Pagos";
    protected override string ExportNombreArchivo => "Pagos.xlsx";

    protected override async Task CargarAuxiliaresAsync()
    {
        // En paralelo: cada repositorio crea su propio DbContext (IDbContextFactory).
        var dgayf = LookupService.GetStatusDgayfOpcionesAsync();
        var op = LookupService.GetStatusOpOpcionesAsync();
        var ultimaFecha = PagosService.GetUltimaFechaImputacionAsync();
        await Task.WhenAll(dgayf, op, ultimaFecha);

        _statusDgayfOpciones = dgayf.Result;
        _statusOpOpciones = op.Result;
        _ultimaActualizacion = ultimaFecha.Result;
    }

    protected override async Task<List<PagoViewModel>> ObtenerDatosAsync() =>
        (await PagosService.GetAllAsync()).ToList();

    protected override PagoViewModel NuevaFila() => new() { FechaDevengado = DateTime.Today };

    protected override string DescripcionFila(PagoViewModel item) =>
        $"Devengado {item.TipoDev} {item.NroDev}";

    protected override string TextoConfirmacionEliminar(PagoViewModel item) =>
        $"Se eliminará la fila del devengado {item.TipoDev} {item.NroDev} " +
        $"(expediente {item.Expediente}, importe {item.Importe:N2}) junto con sus datos " +
        "cargados (status, observaciones, fechas). Esta acción no se puede deshacer.";

    protected override void PrepararParaGuardar(PagoViewModel item)
    {
        // Nombres visibles en la grilla (la edición guarda el Id de la opción).
        item.StatusDgayfNombre = _statusDgayfOpciones.FirstOrDefault(x => x.Id == item.StatusDgayfOpcionId)?.Nombre;
        item.StatusOpNombre = _statusOpOpciones.FirstOrDefault(x => x.Id == item.StatusOpOpcionId)?.Nombre;
    }

    // Solo el alta: en la edición los campos que vienen de IVC son de solo lectura, y las
    // filas históricas del ledger pueden no cumplir las reglas del alta manual.
    protected override IReadOnlyList<string> Validar(PagoViewModel item, bool esAlta) =>
        esAlta ? PagoValidator.ValidarAlta(item) : Array.Empty<string>();

    protected override Task CrearAsync(PagoViewModel item) => PagosService.CreateDevengadoAsync(item);

    protected override Task ActualizarAsync(PagoViewModel item) => PagosService.UpsertAsync(item);

    protected override Task EliminarAsync(PagoViewModel item) => PagosService.DeleteDevengadoAsync(item.Id);

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
                    await ReloadAsync();
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
}