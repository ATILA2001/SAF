#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using SAF.Application.Common;
using SAF.Services.Abstractions;
using SAF.Shared;
using System.Reflection;

namespace SAF.Components;

/// <summary>
/// Clase base de las páginas de grilla (Pagos, Status Contabilidad, CAF, Seguros).
/// Concentra el ciclo completo: carga con manejo de error, alta/edición/baja con
/// revalidación server-side de permisos, confirmación de borrado y export.
/// Las páginas solo declaran qué entidad manejan y cómo se obtiene/persiste.
/// </summary>
public abstract class GridPageBase<TItem> : PermissionPageBase, IDisposable where TItem : class, new()
{
    [Inject] protected INotificationHelper Notification { get; set; } = null!;
    [Inject] private ILogger<GridPageBase<TItem>> Logger { get; set; } = null!;
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IAuditoriaService Auditoria { get; set; } = null!;

    protected RadzenDataGrid<TItem> _grid = null!;
    protected List<TItem> _items = new();
    protected bool _loading = true;
    protected bool _exportando;

    // Carga progresiva: el primer lote pinta la grilla enseguida y el resto llega en
    // segundo plano. Mientras _cargandoResto está activo, el export queda deshabilitado
    // (exportaría una lista incompleta).
    protected bool _cargandoResto;

    // La carga de fondo falló (p. ej. IVC caída): el export queda bloqueado hasta
    // recargar, porque saldría con columnas derivadas vacías que en la semántica de
    // la planilla se leen como "sin pagar / sin pase".
    protected bool _cargaIncompleta;

    /// <summary>Condición compartida del botón Exportar (la usa GridToolbar en las 4 vistas).</summary>
    protected bool ExportarDeshabilitado => _items.Count == 0 || _exportando || _cargandoResto || _cargaIncompleta;

    /// <summary>
    /// Explicación del Exportar deshabilitado, para el tooltip del botón (mismos
    /// motivos, en el mismo orden, que los avisos server-side de ExportarExcel).
    /// </summary>
    protected string? MotivoExportarDeshabilitado =>
        _cargandoResto ? "Esperá a que termine de cargar la grilla para exportar."
        : _cargaIncompleta ? "La carga no se completó: recargá la vista antes de exportar."
        : _items.Count == 0 ? "No hay filas para exportar."
        : null;

    // Búsqueda rápida de la toolbar: filtra en memoria sobre los campos de identidad
    // (CamposBusqueda), sin pasar por los popups de filtro de columna.
    protected string _textoBusqueda = string.Empty;

    private CancellationTokenSource? _ctsLotes;

    // Vida de la página: cancela la carga inicial (y, encadenado, la de fondo) si el
    // usuario navega antes de que termine — sin esto, la continuación relanzaba el
    // trabajo completo para una grilla que ya no existía.
    private readonly CancellationTokenSource _ctsPagina = new();

    // Los editores escriben sobre una COPIA de la fila (el buffer) y no sobre el ítem
    // real: la vista filtrada de Radzen se re-evalúa en cada render, así que si los
    // editores tocaran la fila, cambiar un campo filtrado la sacaría de la vista a
    // mitad de la edición. El ítem real se pisa con el buffer recién al guardar
    // (y cancelar es simplemente descartar el buffer: no hay nada que restaurar).
    private readonly Dictionary<TItem, TItem> _buffers = new();

    // Estado previo al guardado (capturado justo antes de aplicar el buffer):
    // es la base del diff de auditoría en OnRowUpdate.
    private readonly Dictionary<TItem, TItem> _originales = new();

    // Se excluyen las columnas que muta el completado en segundo plano: si al guardar
    // el buffer las pisara con los valores viejos que copió al abrir la edición, un
    // completado que llegó en el medio se perdería de la pantalla hasta la próxima
    // recarga (el usuario no puede editarlas de todos modos).
    private static readonly PropertyInfo[] PropiedadesCopiables =
        typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && p.CanWrite
                         && p.GetCustomAttribute<RestoreIgnoreAttribute>() is null)
                     .ToArray();

    /// <summary>Ruta de la página, para resolver permisos (ej: "/caf").</summary>
    protected abstract string PageUrl { get; }

    /// <summary>Nombre de la entidad en plural, para los mensajes ("expedientes CAF").</summary>
    protected abstract string TituloEntidad { get; }

    protected abstract string ExportNombreHoja { get; }
    protected abstract string ExportNombreArchivo { get; }

    protected abstract Task<List<TItem>> ObtenerDatosAsync();

    /// <summary>
    /// Activa la carga progresiva (primer lote inmediato + resto en segundo plano).
    /// Requiere implementar ObtenerLoteAsync. El default mantiene la carga completa con
    /// ObtenerDatosAsync (p. ej. el tablero, que agrupa en memoria y no pagina por filas).
    /// </summary>
    protected virtual bool CargaProgresiva => false;

    /// <summary>Página del origen de datos con orden determinístico.</summary>
    protected virtual Task<List<TItem>> ObtenerLoteAsync(int skip, int take, CancellationToken ct)
        => throw new NotSupportedException($"{GetType().Name} activó CargaProgresiva sin implementar ObtenerLoteAsync.");

    /// <summary>
    /// Activa la fase de completado: la grilla pinta primero con los datos rápidos
    /// (tablas propias) y esta fase rellena después los que vienen de fuentes lentas
    /// (IVC). Requiere implementar CompletarAsync.
    /// </summary>
    protected virtual bool CompletaEnSegundoPlano => false;

    /// <summary>
    /// Rellena sobre los ítems ya pintados los datos de fuentes lentas. Corre en
    /// segundo plano con la grilla visible; al terminar se refresca.
    /// </summary>
    protected virtual Task CompletarAsync(List<TItem> items, CancellationToken ct)
        => throw new NotSupportedException($"{GetType().Name} activó CompletaEnSegundoPlano sin implementar CompletarAsync.");

    /// <summary>Tamaño del primer lote: chico, para que la primera pintada sea inmediata.</summary>
    protected virtual int TamanoPrimerLote => 100;

    /// <summary>Tamaño de los lotes de fondo.</summary>
    protected virtual int TamanoLote => 500;

    /// <summary>Lookups y datos de encabezado. Corre antes de la carga de la grilla.</summary>
    protected virtual Task CargarAuxiliaresAsync() => Task.CompletedTask;

    /// <summary>
    /// Campos que recorre la búsqueda rápida (los que identifican la fila: expediente,
    /// empresa, etc.). Lista vacía = la página no ofrece búsqueda.
    /// </summary>
    protected virtual IEnumerable<string?> CamposBusqueda(TItem item) => Array.Empty<string?>();

    /// <summary>
    /// Datos de la grilla: los ítems cargados, filtrados por la búsqueda rápida si hay
    /// texto. Los filtros por columna de Radzen se aplican después, sobre este conjunto.
    /// </summary>
    protected IEnumerable<TItem> ItemsVisibles =>
        string.IsNullOrWhiteSpace(_textoBusqueda)
            ? _items
            : _items.Where(item => CamposBusqueda(item)
                .Any(v => v?.Contains(_textoBusqueda.Trim(), StringComparison.OrdinalIgnoreCase) == true));

    /// <summary>Aplica el texto de búsqueda y recalcula la vista (filtros y totales incluidos).</summary>
    protected async Task OnBusquedaChanged(string texto)
    {
        _textoBusqueda = texto;
        if (_grid is not null) await _grid.Reload();
    }

    /// <summary>
    /// Handler vacío para Filter/FilterCleared de la grilla: al ser un callback de la
    /// página, su disparo re-renderiza la toolbar (el estado del botón Quitar filtros
    /// depende de los filtros y la grilla no avisa de otro modo).
    /// </summary>
    protected void OnGridFilter(DataGridColumnFilterEventArgs<TItem> args) { }

    /// <summary>
    /// Hay algo para limpiar: búsqueda rápida o filtros de columna (los popups de
    /// filtro escriben FilterValue/SecondFilterValue). Habilita el botón de la toolbar.
    /// </summary>
    protected bool HayFiltrosActivos =>
        !string.IsNullOrWhiteSpace(_textoBusqueda)
        || (_grid?.ColumnsCollection.Any(c => c.GetFilterValue() is not null || c.GetSecondFilterValue() is not null) ?? false);

    /// <summary>Fila en blanco del alta inline.</summary>
    protected virtual TItem NuevaFila() => new();

    /// <summary>Ajustes sobre la fila antes de persistirla (ej: nombres visibles de los lookups).</summary>
    protected virtual void PrepararParaGuardar(TItem item) { }

    /// <summary>Descripción de la fila para los mensajes de alta/baja y la confirmación.</summary>
    protected virtual string DescripcionFila(TItem item) => string.Empty;

    /// <summary>
    /// Reglas de la fila (lista vacía = válida). Mismas reglas que aplica el servicio:
    /// acá solo se anticipan para no cerrar la edición y perder lo cargado.
    /// </summary>
    protected virtual IReadOnlyList<string> Validar(TItem item, bool esAlta) => Array.Empty<string>();

    protected virtual Task CrearAsync(TItem item) => throw new NotSupportedException();
    protected virtual Task ActualizarAsync(TItem item) => throw new NotSupportedException();
    protected virtual Task EliminarAsync(TItem item) => throw new NotSupportedException();

    protected virtual string TextoConfirmacionEliminar(TItem item) =>
        $"Se eliminará {DescripcionFila(item)}. Esta acción no se puede deshacer.";

    /// <summary>Id técnico de la fila para el historial (null: se busca por clave de negocio).</summary>
    protected virtual int? IdAuditoria(TItem item) => null;

    /// <summary>Clave de negocio estable de la fila para el historial.</summary>
    protected virtual string ClaveAuditoria(TItem item) => DescripcionFila(item);

    /// <summary>
    /// Registra el lote de cambios de un guardado exitoso. La auditoría nunca voltea
    /// el guardado que audita: una falla acá se loguea y la operación sigue.
    /// </summary>
    private async Task RegistrarAuditoriaAsync(string accion, TItem item, IReadOnlyList<AuditoriaDiff.Cambio> cambios)
    {
        if (cambios.Count == 0) return;
        try
        {
            await Auditoria.RegistrarAsync(new RegistroAuditoria(
                PageUrl, IdAuditoria(item), ClaveAuditoria(item), accion, NombreUsuario, cambios));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "No se pudo auditar {Accion} en {Pagina}.", accion, PageUrl);
        }
    }

    /// <summary>Abre el historial de cambios de la fila.</summary>
    protected async Task VerHistorial(TItem item)
    {
        // La UI ya lo esconde; esta es la barrera server-side, como en el resto de
        // los handlers (ver el historial requiere poder leer la vista).
        if (!CanAccess)
        {
            Notification.ShowError($"No tenés permiso para ver {TituloEntidad}.", "Permiso denegado");
            return;
        }

        await DialogService.OpenAsync<Shared.HistorialAuditoria>(
            $"Historial — {DescripcionFila(item)}",
            new Dictionary<string, object?>
            {
                [nameof(Shared.HistorialAuditoria.Vista)] = PageUrl,
                [nameof(Shared.HistorialAuditoria.EntidadId)] = IdAuditoria(item),
                [nameof(Shared.HistorialAuditoria.ClaveNegocio)] = ClaveAuditoria(item),
            },
            new DialogOptions { Width = "720px" });
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadPermissionsAsync(PageUrl);

        // Sin permiso de lectura no se consulta la base: el redirect del MainLayout corre
        // después de que la página inicializa, así que no alcanza como única barrera.
        if (!CanAccess)
        {
            _loading = false;
            return;
        }

        try
        {
            // Los auxiliares (lookups) alimentan los dropdowns de la grilla: se cargan
            // antes de los datos para que la primera pintada ya tenga las opciones.
            await CargarAuxiliaresAsync();
            await CargarDatosAsync();
        }
        catch (OperationCanceledException)
        {
            // El usuario navegó antes de que terminara la carga inicial.
        }
        catch (Exception ex)
        {
            Informar(ex, "La carga", $"Error al cargar {TituloEntidad}");
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Carga _items: completa (ObtenerDatosAsync) o progresiva si la página implementa
    /// ObtenerLoteAsync. Cancela cualquier carga de fondo anterior antes de empezar.
    /// </summary>
    private async Task CargarDatosAsync()
    {
        _ctsLotes?.Cancel();
        _ctsLotes?.Dispose();
        _ctsLotes = null;
        _cargandoResto = false;
        _cargaIncompleta = false;

        bool faltanLotes;
        if (CargaProgresiva)
        {
            _items = await ObtenerLoteAsync(0, TamanoPrimerLote, _ctsPagina.Token);
            faltanLotes = _items.Count >= TamanoPrimerLote;
        }
        else
        {
            _items = await ObtenerDatosAsync();
            faltanLotes = false;
        }

        // Si el usuario navegó mientras esperábamos el primer resultado, no se lanza
        // trabajo de fondo para una grilla que ya no existe.
        if (_ctsPagina.IsCancellationRequested) return;

        if (!faltanLotes && !CompletaEnSegundoPlano) return;

        // Encadenado a la vida de la página: navegar cancela también los lotes.
        _ctsLotes = CancellationTokenSource.CreateLinkedTokenSource(_ctsPagina.Token);
        _cargandoResto = true;
        _ = SegundoPlanoAsync(_items, faltanLotes, _ctsLotes.Token);
    }

    /// <summary>
    /// Trabajo de fondo con la grilla ya visible: trae los lotes restantes y después
    /// rellena los datos de fuentes lentas (una sola pasada, con todas las filas).
    /// </summary>
    private async Task SegundoPlanoAsync(List<TItem> destino, bool faltanLotes, CancellationToken ct)
    {
        try
        {
            // Dedupe entre lotes: si otra sesión inserta o borra mientras se pagina,
            // el Skip/Take se corre y una fila puede venir dos veces. La clave es el
            // Id de auditoría (las páginas progresivas lo exponen).
            var idsVistos = new HashSet<int>(destino.Select(IdAuditoria).OfType<int>());

            var skip = destino.Count;
            while (faltanLotes && !ct.IsCancellationRequested)
            {
                var lote = await ObtenerLoteAsync(skip, TamanoLote, ct);

                // Si hubo una recarga en el medio, _items es otra lista: este runner
                // quedó viejo y no debe mezclar sus filas con las nuevas.
                if (ct.IsCancellationRequested || !ReferenceEquals(destino, _items)) return;

                var nuevos = lote.Where(x => IdAuditoria(x) is not int id || idsVistos.Add(id)).ToList();
                if (nuevos.Count > 0)
                {
                    // La mutación de la lista corre dentro del dispatcher a propósito:
                    // queda serializada con los renders aunque algún servicio futuro
                    // meta un ConfigureAwait(false) en la cadena.
                    await InvokeAsync(async () =>
                    {
                        destino.AddRange(nuevos);
                        if (_grid is not null) await _grid.Reload();
                        StateHasChanged();
                    });
                }

                skip += lote.Count;
                if (lote.Count < TamanoLote) break;
            }

            if (CompletaEnSegundoPlano && !ct.IsCancellationRequested && ReferenceEquals(destino, _items))
            {
                await CompletarAsync(destino, ct);

                if (!ct.IsCancellationRequested && ReferenceEquals(destino, _items))
                    await InvokeAsync(async () =>
                    {
                        if (_grid is not null) await _grid.Reload();
                        StateHasChanged();
                    });
            }
        }
        catch (OperationCanceledException)
        {
            // Navegación o recarga: no hay nada que informar.
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested && ReferenceEquals(destino, _items))
            {
                _cargaIncompleta = true;
                await InvokeAsync(() => Informar(ex, "La carga de fondo", $"Error al cargar {TituloEntidad}"));
            }
        }
        finally
        {
            // Solo el runner vigente administra el flag: uno cancelado por una recarga
            // no debe pisar el estado del nuevo (dejaría el export sin barrera).
            if (!ct.IsCancellationRequested && ReferenceEquals(destino, _items))
            {
                _cargandoResto = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    public void Dispose()
    {
        _ctsPagina.Cancel();
        _ctsPagina.Dispose();
        _ctsLotes?.Cancel();
        _ctsLotes?.Dispose();
        _ctsLotes = null;
    }

    protected async Task AddRow() => await _grid.InsertRow(NuevaFila());

    /// <summary>Quita la búsqueda rápida y los filtros de columna, y recarga la vista.</summary>
    protected async Task LimpiarFiltros()
    {
        if (_grid is null) return;

        _textoBusqueda = string.Empty;
        foreach (var columna in _grid.ColumnsCollection)
            columna.ClearFilters();

        await _grid.Reload();
    }

    /// <summary>
    /// Buffer de edición de la fila: los EditTemplates bindean acá. Se crea al abrir la
    /// edición (RowEdit) o, para la fila del alta —que no pasa por RowEdit—, en el
    /// primer render de sus editores.
    /// </summary>
    protected TItem Buffer(TItem item)
    {
        if (!_buffers.TryGetValue(item, out var buffer))
            _buffers[item] = buffer = Copiar(item, new TItem());

        return buffer;
    }

    /// <summary>Al abrir la edición se crea el buffer sobre el que escriben los editores.</summary>
    protected void OnRowEdit(TItem item)
    {
        // Con EditMode.Single, abrir otra fila cierra la anterior sin pasar por Cancelar:
        // su buffer se descarta (el ítem real nunca se tocó).
        foreach (var fila in _buffers.Keys.Where(f => !ReferenceEquals(f, item)).ToList())
            _buffers.Remove(fila);

        _buffers[item] = Copiar(item, new TItem());
    }

    protected void CancelEdit(TItem item)
    {
        // El ítem real nunca se tocó: cancelar es descartar el buffer.
        _buffers.Remove(item);
        _grid.CancelEditRow(item);
    }

    /// <summary>
    /// Registra el detalle técnico y le muestra al usuario algo accionable. Las
    /// ArgumentException son validaciones escritas para él; el resto (SQL, EF) no le
    /// sirve y expondría nombres de tablas y constraints.
    /// </summary>
    protected void Informar(Exception ex, string accion, string titulo)
    {
        Logger.LogError(ex, "{Accion} falló en {Pagina}.", accion, PageUrl);

        Notification.ShowError(
            ex is ArgumentException ? ex.Message : "Ocurrió un error inesperado. Si persiste, avisá a Sistemas.",
            titulo);
    }

    private static TItem Copiar(TItem origen, TItem destino)
    {
        foreach (var propiedad in PropiedadesCopiables)
            propiedad.SetValue(destino, propiedad.GetValue(origen));

        return destino;
    }

    /// <summary>
    /// Guarda la fila en edición. Valida el buffer ANTES de delegar en la grilla: si
    /// Radzen confirma la fila, la cierra y lo cargado se pierde, así que el error tiene
    /// que frenar antes. El ítem real recién se pisa cuando el guardado va en serio.
    /// </summary>
    protected async Task GuardarFila(TItem item)
    {
        // La fila del alta todavía no está en la lista: eso la distingue de una edición.
        var esAlta = !_items.Contains(item);
        var buffer = Buffer(item);
        if (!FilaValida(buffer, esAlta)) return;

        // Se pregunta acá, antes de que la grilla cierre la fila: si el usuario cancela,
        // conserva lo que cargó. Una falla (la confirmación puede consultar la base)
        // tiene que dejar la fila en edición, no voltear el circuito.
        try
        {
            if (!await ConfirmarGuardadoAsync(buffer, esAlta)) return;
        }
        catch (Exception ex)
        {
            Informar(ex, "El guardado", "Error al guardar");
            return;
        }

        // Recién acá se aplica lo tipeado sobre la fila real, guardando antes el estado
        // previo para el diff de auditoría de OnRowUpdate.
        _originales[item] = Copiar(item, new TItem());
        Copiar(buffer, item);
        _buffers.Remove(item);

        await _grid.UpdateRow(item);
    }

    /// <summary>
    /// Última pregunta antes de guardar, para casos sospechosos pero permitidos.
    /// Devolver false cancela el guardado dejando la fila en edición.
    /// </summary>
    protected virtual Task<bool> ConfirmarGuardadoAsync(TItem item, bool esAlta) => Task.FromResult(true);

    protected async Task OnRowCreate(TItem item)
    {
        // La UI esconde el botón, pero el permiso se revalida acá (server-side).
        if (!CanCreate)
        {
            Notification.ShowError($"No tenés permiso para crear {TituloEntidad}.", "Permiso denegado");
            return;
        }

        if (!FilaValida(item, esAlta: true)) return;

        // Igual que en la edición: resuelve los nombres visibles de los lookups, que
        // el snapshot de auditoría del alta necesita (los servicios leen solo los Ids).
        PrepararParaGuardar(item);

        try
        {
            await CrearAsync(item);
            await RegistrarAuditoriaAsync("Alta", item, AuditoriaDiff.Snapshot(item, esBaja: false));
            await ReloadAsync();
            Notification.ShowSuccess($"{DescripcionFila(item)} creado.".TrimStart(), "Alta exitosa");
        }
        catch (Exception ex)
        {
            Informar(ex, "El alta", "Error al crear");
        }
    }

    protected async Task OnRowUpdate(TItem item)
    {
        if (!CanEdit)
        {
            Notification.ShowError($"No tenés permiso para editar {TituloEntidad}.", "Permiso denegado");
            return;
        }

        if (!FilaValida(item, esAlta: false)) return;

        PrepararParaGuardar(item);

        // La copia previa de la fila (guardada al abrir la edición) es la base del diff
        // de auditoría; se captura antes de que ReloadAsync limpie el diccionario.
        _originales.TryGetValue(item, out var original);

        try
        {
            await ActualizarAsync(item);
            if (original is not null)
                await RegistrarAuditoriaAsync("Edición", item, AuditoriaDiff.Comparar(original, item));
            await ReloadAsync();
        }
        catch (ConflictoDeConcurrenciaException ex)
        {
            // Se recarga para que el usuario vea lo que quedó realmente guardado antes
            // de decidir si vuelve a aplicar su cambio.
            Logger.LogWarning("Conflicto de concurrencia al guardar en {Pagina}.", PageUrl);
            Notification.ShowWarning(ex.Message, "Fila desactualizada");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Informar(ex, "El guardado", "Error al guardar");
        }
    }

    protected async Task DeleteRow(TItem item)
    {
        if (!CanDelete)
        {
            Notification.ShowError($"No tenés permiso para eliminar {TituloEntidad}.", "Permiso denegado");
            return;
        }

        var confirmado = await DialogService.Confirm(
            TextoConfirmacionEliminar(item),
            "Confirmar eliminación",
            new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar" });
        if (confirmado != true) return;

        try
        {
            await EliminarAsync(item);
            // El snapshot completo es lo que permite responder "¿qué decía la fila borrada?".
            await RegistrarAuditoriaAsync("Baja", item, AuditoriaDiff.Snapshot(item, esBaja: true));
            await ReloadAsync();
            Notification.ShowSuccess($"{DescripcionFila(item)} eliminado.".TrimStart(), "Baja exitosa");
        }
        catch (ConflictoDeConcurrenciaException ex)
        {
            // La confirmación se basó en datos que ya cambiaron: se recarga para que el
            // usuario vea el estado real antes de decidir si igual quiere borrar.
            Logger.LogWarning("Conflicto de concurrencia al eliminar en {Pagina}.", PageUrl);
            Notification.ShowWarning(ex.Message, "Fila desactualizada");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Informar(ex, "La baja", "Error al eliminar");
        }
    }

    /// <summary>
    /// Valida la fila y avisa qué falta. Es advertencia y no error: se cierra sola, porque
    /// el dato inválido ya queda señalado en el campo (clase "invalid") mientras se edita.
    /// </summary>
    private bool FilaValida(TItem item, bool esAlta)
    {
        var errores = Validar(item, esAlta);
        if (errores.Count == 0) return true;

        Notification.ShowWarning(string.Join(" ", errores), "Revisá los datos");
        return false;
    }

    /// <summary>
    /// Re-consulta y refresca la grilla. Tras guardar también se recarga: las columnas
    /// derivadas (cruces por expediente) cambian con la edición y quedarían desfasadas.
    /// </summary>
    protected async Task ReloadAsync()
    {
        _buffers.Clear();
        _originales.Clear();
        await CargarDatosAsync();
        await _grid.Reload();
    }

    protected async Task ExportarExcel()
    {
        if (!CanAccess)
        {
            Notification.ShowError($"No tenés permiso para exportar {TituloEntidad}.", "Permiso denegado");
            return;
        }

        // El botón ya se deshabilita durante la carga de fondo; esta es la barrera
        // server-side para no exportar una lista incompleta.
        if (_cargandoResto)
        {
            Notification.ShowWarning("Esperá a que termine de cargar la grilla para exportar.", "Carga en curso");
            return;
        }

        if (_cargaIncompleta)
        {
            Notification.ShowWarning(
                "La carga no se completó (falló una consulta): recargá la vista antes de exportar.",
                "Datos incompletos");
            return;
        }

        _exportando = true;
        try
        {
            // View trae los filtros y el orden de la grilla, pero Radzen lo deja apuntando
            // solo a la página actual después de insertar o cancelar una fila
            // (InsertRowAtIndex / CancelEditRow reasignan _view = PagedView). Recargar lo
            // recalcula desde los datos; sin esto el Excel saldría con una sola página.
            if (_grid is not null) await _grid.Reload();
            var filas = _grid?.View?.ToList() ?? _items;

            // La generación es CPU puro y sincrónica: en un hilo del pool no bloquea el
            // circuito, y al ser el primer await que suspende de verdad, deja que Blazor
            // pinte el estado "Exportando..." del botón antes de ponerse a trabajar.
            var bytes = await Task.Run(() => ExportService.ExportToXlsx(filas, ExportNombreHoja));

            // El archivo viaja como stream (Blazor lo trocea en chunks por SignalR):
            // sin base64 no hay +33% en la red, ni string gigante en el heap, ni un
            // único mensaje que congele el circuito mientras se transfiere.
            using var stream = new MemoryStream(bytes);
            using var streamRef = new DotNetStreamReference(stream);
            await JS.InvokeVoidAsync("downloadFileFromStream", ExportNombreArchivo, streamRef,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            Notification.ShowSuccess($"Se exportaron {filas.Count} filas.", "Exportación completada");
        }
        catch (Exception ex)
        {
            Informar(ex, "El export", "Error al exportar");
        }
        finally
        {
            _exportando = false;
        }
    }
}