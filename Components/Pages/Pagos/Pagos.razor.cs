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

    // Búsqueda rápida por los campos que identifican la fila.
    protected override IEnumerable<string?> CamposBusqueda(PagoViewModel item) =>
        [item.TipoDev, item.NroDev.ToString(), item.Expediente, item.Empresa];

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

    // Cada fila del ledger tiene historial propio (un devengado puede tener varias líneas).
    protected override int? IdAuditoria(PagoViewModel item) => item.Id != 0 ? item.Id : null;

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

    /// <summary>
    /// Alta en ventana (reemplaza al alta inline en la grilla): el panel de identidad
    /// se completa en vivo con lo tipeado. Cancelar descarta el objeto solo — la fila
    /// no existe hasta que CrearDesdeDialogoAsync la persiste.
    /// </summary>
    private async Task NuevoEnVentana()
    {
        var item = NuevaFila();

        await DialogService.OpenAsync<PagoEditor>(
            "Nuevo devengado",
            new Dictionary<string, object?>
            {
                // El mismo objeto como Item y Borrador: el panel refleja lo que se tipea.
                [nameof(PagoEditor.Item)] = item,
                [nameof(PagoEditor.Borrador)] = item,
                [nameof(PagoEditor.EsAlta)] = true,
                [nameof(PagoEditor.StatusDgayfOpciones)] = _statusDgayfOpciones,
                [nameof(PagoEditor.StatusOpOpciones)] = _statusOpOpciones,
                [nameof(PagoEditor.GuardarAsync)] = (Func<Task<bool>>)(() => CrearDesdeDialogoAsync(item)),
            },
            new DialogOptions { Width = "1000px", ShowTitle = false, CssClass = "saf-dialog-panel" });
    }

    /// <summary>
    /// Edición en ventana: los campos editables a la vista, con labels. Comparte el
    /// borrador con la edición inline; cerrar sin guardar lo descarta.
    /// </summary>
    private async Task EditarEnVentana(PagoViewModel item)
    {
        var resultado = await DialogService.OpenAsync<PagoEditor>(
            $"Editar — Devengado {item.TipoDev} {item.NroDev}",
            new Dictionary<string, object?>
            {
                [nameof(PagoEditor.Item)] = item,
                [nameof(PagoEditor.Borrador)] = Buffer(item),
                [nameof(PagoEditor.StatusDgayfOpciones)] = _statusDgayfOpciones,
                [nameof(PagoEditor.StatusOpOpciones)] = _statusOpOpciones,
                [nameof(PagoEditor.GuardarAsync)] = (Func<Task<bool>>)(() => GuardarDesdeDialogoAsync(item)),
            },
            // Sin barra de título de Radzen: el editor pone su propio encabezado y su
            // panel de identidad va de borde a borde (padding 0 vía saf-dialog-panel).
            new DialogOptions { Width = "1000px", ShowTitle = false, CssClass = "saf-dialog-panel" });

        // Cerrado sin guardar (Cancelar, la X o Escape): se descarta lo tipeado.
        if (resultado is not true) DescartarBuffer(item);
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