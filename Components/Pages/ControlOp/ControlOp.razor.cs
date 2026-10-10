#nullable enable
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;
using SAF.Services.Abstractions;

namespace SAF.Components.Pages.ControlOp;

/// <summary>
/// La hoja "control OP" del Excel en dos pestañas: la lista de devengados con OP firmada
/// que no cruzan con ninguna OP pagada (solo lectura, con la fecha de corte que antes
/// vivía en la celda A1) y las correcciones de OP, que es la grilla editable de la
/// página (GridPageBase: alta inline, edición, baja, historial y export). Desde la lista
/// se busca la OP en la bajada y se la asigna al expediente del devengado.
/// </summary>
public partial class ControlOp
{
    [Inject] private IControlOpService Servicio { get; set; } = null!;

    protected override string PageUrl => "/control-op";
    protected override string TituloEntidad => "correcciones de OP";
    protected override string ExportNombreHoja => "Correcciones OP";
    protected override string ExportNombreArchivo => "CorreccionesOp.xlsx";

    // ─── Lista de control (solo lectura; la base maneja la grilla de correcciones) ──

    private DateTime _fechaCorte = ReglasControlOp.FechaCorte(DateTime.Today);
    private List<ControlOpViewModel> _lista = new();
    // Nace cargando: el primer render ocurre mientras la base carga, y una grilla vacía
    // con el contador en (0) se leería como "no hay devengados pendientes".
    private bool _cargandoLista = true;
    private bool _listaIncompleta;
    private bool _exportandoLista;
    private RadzenDataGrid<ControlOpViewModel>? _gridLista;

    // Dos cargas de la lista pueden solaparse (guardados seguidos, cambio de fecha
    // mientras carga): solo el resultado de la última cuenta.
    private int _generacionLista;

    // Sin contador mientras carga o si la carga falló: "(0)" se leería como "nada pendiente".
    private string TituloTabLista => _cargandoLista || _listaIncompleta ? "Sin OP relacionada" : $"Sin OP relacionada ({_lista.Count})";
    private string TituloTabCorrecciones => _loading ? "Correcciones" : $"Correcciones ({_items.Count})";

    // Filtros, orden y anchos de la lista sobreviven a la navegación como los de toda
    // grilla, bajo su propia clave (la base guarda los de las correcciones bajo PageUrl).
    private string ClaveEstadoLista => PageUrl + "#lista";
    private void GuardarSettingsLista(DataGridSettings settings) => EstadoGrillas.De(ClaveEstadoLista).Settings = settings;
    private void CargarSettingsLista(DataGridLoadSettingsEventArgs args)
    {
        if (EstadoGrillas.De(ClaveEstadoLista).Settings is { } guardados)
            args.Settings = guardados;
    }

    private bool ExportarListaDeshabilitado => _lista.Count == 0 || _cargandoLista || _listaIncompleta || _exportandoLista;

    private string? MotivoExportarListaDeshabilitado =>
        _cargandoLista ? "Esperá a que termine de cargar la lista para exportar."
        : _listaIncompleta ? "La lista no se cargó completa: actualizala antes de exportar."
        : _lista.Count == 0 ? "No hay filas para exportar."
        : null;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (!CanAccess)
        {
            _cargandoLista = false;
            return;
        }

        // Pintar ya la grilla de correcciones (sale de tablas propias) y recién después
        // ir a IVC por la lista: Blazor no vuelve a renderizar hasta que termina
        // OnInitializedAsync, así que sin esto la primera pintada esperaba a IVC.
        StateHasChanged();
        await CargarListaAsync();
    }

    private async Task CargarListaAsync()
    {
        // Encadenada a la vida de la página: navegar cancela la consulta a IVC en vez
        // de dejarla correr y volver sobre un componente ya descartado.
        var ct = TokenPagina;
        var generacion = ++_generacionLista;
        _cargandoLista = true;
        _listaIncompleta = false;
        try
        {
            var lista = (await Servicio.GetListaControlAsync(_fechaCorte, ct)).ToList();
            if (generacion != _generacionLista) return; // ya arrancó otra carga: esta quedó vieja

            _lista = lista;
            // La grilla queda montada durante la carga (IsLoading): el Reload refresca
            // la vista sin perder los filtros de columna que el usuario aplicó.
            if (_gridLista is not null) await _gridLista.Reload();
        }
        catch (OperationCanceledException)
        {
            return; // el usuario navegó: no queda nadie mirando
        }
        catch (Exception ex)
        {
            if (generacion != _generacionLista) return;
            // IVC caída, por ejemplo: la pestaña de correcciones sigue sirviendo.
            _listaIncompleta = true;
            Informar(ex, "La lista de control", "Error al cargar la lista de control");
        }
        finally
        {
            if (generacion == _generacionLista) _cargandoLista = false;
        }

        // Puede correr fuera de un handler (refresco en segundo plano tras guardar):
        // se pinta explícitamente.
        if (!ct.IsCancellationRequested) await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Toda mutación de una corrección mueve la lista de control: el devengado cuyo
    /// expediente ahora cruza con la OP deja de estar pendiente, o vuelve a estarlo.
    /// Sin esperar: la grilla de correcciones se refresca y avisa enseguida, y la
    /// lista llega cuando IVC responde.
    /// </summary>
    private void RefrescarListaEnSegundoPlano() => _ = CargarListaAsync();

    private async Task OnFechaCorteChanged(DateTime? valor)
    {
        // Vaciar el campo vuelve al corte por defecto (hoy menos los días de regla).
        _fechaCorte = valor?.Date ?? ReglasControlOp.FechaCorte(DateTime.Today);
        await CargarListaAsync();
    }

    /// <summary>
    /// Abre la búsqueda en la bajada para el devengado y, si el usuario elige una OP,
    /// la asigna a su expediente por la misma tubería que el alta inline (permiso,
    /// validación, CrearAsync con la foto de la bajada, auditoría, recarga y aviso).
    /// </summary>
    private async Task BuscarOp(ControlOpViewModel fila)
    {
        if (!CanCreate)
        {
            Notification.ShowError($"No tenés permiso para crear {TituloEntidad}.", "Permiso denegado");
            return;
        }

        var resultado = await DialogService.OpenAsync<BuscarOpDialog>(
            $"Buscar orden de pago — Devengado {fila.TipoDev} {fila.NroDev}",
            new Dictionary<string, object?> { [nameof(BuscarOpDialog.Fila)] = fila },
            // Mismo esquema que los editores en ventana: sin barra de título de Radzen,
            // panel de identidad de borde a borde (padding 0 vía saf-dialog-panel).
            new DialogOptions { Width = "1150px", ShowTitle = false, CssClass = "saf-dialog-panel" });

        // Cualquier otro cierre (Cancelar, la X, Escape) no asigna nada.
        if (resultado is not CandidataOpViewModel candidata) return;

        var confirmado = await DialogService.Confirm(
            $"Se asignará la OP {candidata.Op} (hoy en la bajada de OP con expediente " +
            $"{candidata.ExpedienteSigaf ?? "(vacío)"}) al expediente {fila.Expediente}. " +
            "En Pagos pasa a contar como pago de ese expediente y deja de contar para el anterior.",
            "Confirmar corrección",
            new ConfirmOptions { OkButtonText = "Asignar", CancelButtonText = "Cancelar" });
        if (confirmado != true) return;

        await CrearDesdeDialogoAsync(new CorreccionOpViewModel
        {
            NroDocResp = candidata.NroDocResp ?? 0,
            Op = candidata.Op,
            ExpedienteCorregido = fila.Expediente ?? string.Empty,
        });
    }

    private async Task ExportarLista()
    {
        // La UI ya deshabilita el botón; esta es la barrera server-side, con el mismo motivo.
        if (!CanAccess || ExportarListaDeshabilitado)
        {
            Notification.ShowWarning(
                MotivoExportarListaDeshabilitado ?? "No tenés permiso para exportar la lista de control.",
                "No se puede exportar");
            return;
        }

        _exportandoLista = true;
        try
        {
            // Lo que queda a la vista con los filtros aplicados, como en las otras grillas.
            if (_gridLista is not null) await _gridLista.Reload();
            var filas = _gridLista?.View?.ToList() ?? _lista;
            await DescargarXlsxAsync(filas, "Control OP", "ControlOp.xlsx");
        }
        catch (Exception ex)
        {
            Informar(ex, "El export de la lista de control", "Error al exportar");
        }
        finally
        {
            _exportandoLista = false;
        }
    }

    // ─── Correcciones (grilla editable de GridPageBase) ──────────────────────

    protected override async Task<List<CorreccionOpViewModel>> ObtenerDatosAsync() =>
        (await Servicio.GetCorreccionesAsync()).ToList();

    protected override IEnumerable<string?> CamposBusqueda(CorreccionOpViewModel item) =>
        [item.Op, item.NroDocResp.ToString(), item.ExpedienteOriginal, item.ExpedienteCorregido, item.Beneficiario];

    protected override async Task<CorreccionOpViewModel?> ObtenerFilaAsync(CorreccionOpViewModel item, CancellationToken ct) =>
        item.Id != 0 ? await Servicio.GetCorreccionByIdAsync(item.Id, ct) : null;

    protected override string DescripcionFila(CorreccionOpViewModel item) => $"Corrección OP {item.Op}";

    protected override int? IdAuditoria(CorreccionOpViewModel item) => item.Id != 0 ? item.Id : null;

    protected override string TextoConfirmacionEliminar(CorreccionOpViewModel item) =>
        $"Se eliminará la corrección de la OP {item.Op}: vuelve a contar para el expediente " +
        $"{item.ExpedienteOriginal ?? "(vacío)"} que trae la bajada de OP y deja de contar para {item.ExpedienteCorregido}. " +
        "Esta acción no se puede deshacer.";

    protected override IReadOnlyList<string> Validar(CorreccionOpViewModel item, bool esAlta) =>
        CorreccionOpValidator.Validar(item);

    protected override async Task CrearAsync(CorreccionOpViewModel item)
    {
        // Lo que el servicio completó desde la bajada (foto de la OP, expediente
        // normalizado) vuelve a la fila en pantalla: lo usan la auditoría y el refresco.
        Refrescar(await Servicio.CreateAsync(item), item);
        RefrescarListaEnSegundoPlano();
    }

    protected override async Task ActualizarAsync(CorreccionOpViewModel item)
    {
        await Servicio.UpdateAsync(item);
        RefrescarListaEnSegundoPlano();
    }

    protected override async Task EliminarAsync(CorreccionOpViewModel item)
    {
        if (item.Id == 0) return;
        await Servicio.DeleteAsync(item.Id, item.RowVersion);
        RefrescarListaEnSegundoPlano();
    }
}
