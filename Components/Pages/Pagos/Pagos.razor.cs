using Microsoft.AspNetCore.Components;
using Radzen;
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
    [Inject] private TooltipService TooltipService { get; set; } = null!;

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

    protected override bool CargaProgresiva => true;

    protected override async Task<List<PagoViewModel>> ObtenerLoteAsync(int skip, int take, CancellationToken ct) =>
        (await PagosService.GetPageAsync(skip, take, ct)).ToList();

    // Las columnas IVC (SADE, Fecha Pago No CAF/Total) llegan con la grilla ya pintada.
    protected override bool CompletaEnSegundoPlano => true;

    protected override Task CompletarAsync(List<PagoViewModel> items, CancellationToken ct) =>
        PagosService.CompletarIvcAsync(items, ct);

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

    // En la edición solo se validan los campos propios: los que vienen de IVC son de solo
    // lectura y las filas históricas del ledger pueden no cumplir las reglas del alta.
    protected override IReadOnlyList<string> Validar(PagoViewModel item, bool esAlta) =>
        esAlta ? PagoValidator.ValidarAlta(item) : PagoValidator.ValidarEdicion(item);

    /// <summary>
    /// Una fila repetida es legítima (un devengado puede tener líneas idénticas) pero casi
    /// siempre es una carga duplicada: se pregunta en vez de rechazarla.
    /// </summary>
    protected override async Task<bool> ConfirmarGuardadoAsync(PagoViewModel item, bool esAlta)
    {
        if (!esAlta || !await PagosService.ExisteDevengadoIdenticoAsync(item)) return true;

        var confirmado = await DialogService.Confirm(
            $"Ya existe una fila del devengado {item.TipoDev} {item.NroDev} con la misma fecha " +
            $"e importe ({item.Importe:N2}). Un devengado puede tener líneas repetidas, " +
            "pero revisá que no sea una carga duplicada.",
            "Fila repetida",
            new ConfirmOptions { OkButtonText = "Cargar igual", CancelButtonText = "Revisar" });

        return confirmado == true;
    }

    protected override Task CrearAsync(PagoViewModel item) => PagosService.CreateDevengadoAsync(item);

    protected override Task ActualizarAsync(PagoViewModel item) => PagosService.UpsertAsync(item);

    protected override Task EliminarAsync(PagoViewModel item) =>
        PagosService.DeleteDevengadoAsync(item.Id, item.RowVersion);

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
            Informar(ex, "La sincronización", "Error al sincronizar");
        }
        finally
        {
            _syncing = false;
        }
    }
}