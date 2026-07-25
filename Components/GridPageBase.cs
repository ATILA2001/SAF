#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
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
public abstract class GridPageBase<TItem> : PermissionPageBase where TItem : class, new()
{
    [Inject] protected INotificationHelper Notification { get; set; } = null!;
    [Inject] private ILogger<GridPageBase<TItem>> Logger { get; set; } = null!;
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    protected RadzenDataGrid<TItem> _grid = null!;
    protected List<TItem> _items = new();
    protected bool _loading = true;

    // Los editores de la grilla escriben directo sobre la fila, así que cancelar no
    // alcanza para deshacer: se guarda una copia al abrir la edición y se restaura.
    private readonly Dictionary<TItem, TItem> _originales = new();

    private static readonly PropertyInfo[] PropiedadesCopiables =
        typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && p.CanWrite)
                     .ToArray();

    /// <summary>Ruta de la página, para resolver permisos (ej: "/caf").</summary>
    protected abstract string PageUrl { get; }

    /// <summary>Nombre de la entidad en plural, para los mensajes ("expedientes CAF").</summary>
    protected abstract string TituloEntidad { get; }

    protected abstract string ExportNombreHoja { get; }
    protected abstract string ExportNombreArchivo { get; }

    protected abstract Task<List<TItem>> ObtenerDatosAsync();

    /// <summary>Lookups y datos de encabezado. Corre antes de la carga de la grilla.</summary>
    protected virtual Task CargarAuxiliaresAsync() => Task.CompletedTask;

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
            _items = await ObtenerDatosAsync();
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

    protected async Task AddRow() => await _grid.InsertRow(NuevaFila());

    /// <summary>Al abrir la edición se guarda el estado previo, para poder cancelar.</summary>
    protected void OnRowEdit(TItem item)
    {
        // Con EditMode.Single, abrir otra fila cierra la anterior sin pasar por Cancelar:
        // hay que deshacer lo que quedó a medias y no dejar la copia colgada.
        foreach (var (fila, original) in _originales.ToList())
            if (!ReferenceEquals(fila, item))
            {
                Copiar(original, fila);
                _originales.Remove(fila);
            }

        _originales[item] = Copiar(item, new TItem());
    }

    protected void CancelEdit(TItem item)
    {
        if (_originales.Remove(item, out var original)) Copiar(original, item);
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
    /// Guarda la fila en edición. Valida ANTES de delegar en la grilla: si Radzen confirma
    /// la fila, la cierra y lo cargado se pierde, así que el error tiene que frenar antes.
    /// </summary>
    protected async Task GuardarFila(TItem item)
    {
        // La fila del alta todavía no está en la lista: eso la distingue de una edición.
        if (!FilaValida(item, esAlta: !_items.Contains(item))) return;

        await _grid.UpdateRow(item);
    }

    protected async Task OnRowCreate(TItem item)
    {
        // La UI esconde el botón, pero el permiso se revalida acá (server-side).
        if (!CanCreate)
        {
            Notification.ShowError($"No tenés permiso para crear {TituloEntidad}.", "Permiso denegado");
            return;
        }

        if (!FilaValida(item, esAlta: true)) return;

        try
        {
            await CrearAsync(item);
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

        try
        {
            await ActualizarAsync(item);
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
            await ReloadAsync();
            Notification.ShowSuccess($"{DescripcionFila(item)} eliminado.".TrimStart(), "Baja exitosa");
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
        _originales.Clear();
        _items = await ObtenerDatosAsync();
        await _grid.Reload();
    }

    protected async Task ExportarExcel()
    {
        if (!CanAccess)
        {
            Notification.ShowError($"No tenés permiso para exportar {TituloEntidad}.", "Permiso denegado");
            return;
        }

        try
        {
            // View trae los filtros y el orden de la grilla, pero Radzen lo deja apuntando
            // solo a la página actual después de insertar o cancelar una fila
            // (InsertRowAtIndex / CancelEditRow reasignan _view = PagedView). Recargar lo
            // recalcula desde los datos; sin esto el Excel saldría con una sola página.
            if (_grid is not null) await _grid.Reload();
            var filas = _grid?.View?.ToList() ?? _items;

            var bytes = ExportService.ExportToXlsx(filas, ExportNombreHoja);
            var base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("downloadFileFromBase64", base64, ExportNombreArchivo,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            Informar(ex, "El export", "Error al exportar");
        }
    }
}