#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using SAF.Services.Abstractions;
using SAF.Shared;

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
    [Inject] private IExportService ExportService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    protected RadzenDataGrid<TItem> _grid = null!;
    protected List<TItem> _items = new();
    protected bool _loading = true;

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

        try
        {
            // Secuencial: comparten el AppDbContext scoped (EF Core no admite
            // operaciones concurrentes sobre la misma instancia de DbContext).
            await CargarAuxiliaresAsync();
            _items = await ObtenerDatosAsync();
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, $"Error al cargar {TituloEntidad}");
        }
        finally
        {
            _loading = false;
        }
    }

    protected async Task AddRow() => await _grid.InsertRow(NuevaFila());

    protected void CancelEdit(TItem item) => _grid.CancelEditRow(item);

    protected async Task OnRowCreate(TItem item)
    {
        // La UI esconde el botón, pero el permiso se revalida acá (server-side).
        if (!CanCreate)
        {
            Notification.ShowError($"No tenés permiso para crear {TituloEntidad}.", "Permiso denegado");
            return;
        }

        if (!await FilaValidaAsync(item, esAlta: true)) return;

        try
        {
            await CrearAsync(item);
            await ReloadAsync();
            Notification.ShowSuccess($"{DescripcionFila(item)} creado.".TrimStart(), "Alta exitosa");
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al crear");
        }
    }

    protected async Task OnRowUpdate(TItem item)
    {
        if (!CanEdit)
        {
            Notification.ShowError($"No tenés permiso para editar {TituloEntidad}.", "Permiso denegado");
            return;
        }

        if (!await FilaValidaAsync(item, esAlta: false)) return;

        PrepararParaGuardar(item);

        try
        {
            await ActualizarAsync(item);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al guardar");
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
            Notification.ShowError(ex.Message, "Error al eliminar");
        }
    }

    /// <summary>
    /// Valida la fila y, si no pasa, reabre la edición con lo que el usuario ya cargó
    /// (Radzen cierra la fila al confirmar, así que hay que volver a abrirla).
    /// </summary>
    private async Task<bool> FilaValidaAsync(TItem item, bool esAlta)
    {
        var errores = Validar(item, esAlta);
        if (errores.Count == 0) return true;

        Notification.ShowError(string.Join(" ", errores), "Revisá los datos");

        try
        {
            if (esAlta) await _grid.InsertRow(item);
            else await _grid.EditRow(item);
        }
        catch
        {
            // Si la grilla no puede reabrir la fila, el error ya quedó informado.
        }

        return false;
    }

    /// <summary>
    /// Re-consulta y refresca la grilla. Tras guardar también se recarga: las columnas
    /// derivadas (cruces por expediente) cambian con la edición y quedarían desfasadas.
    /// </summary>
    protected async Task ReloadAsync()
    {
        _items = await ObtenerDatosAsync();
        await _grid.Reload();
    }

    protected async Task ExportarExcel()
    {
        try
        {
            var bytes = ExportService.ExportToXlsx(_items, ExportNombreHoja);
            var base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("downloadFileFromBase64", base64, ExportNombreArchivo,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            Notification.ShowError(ex.Message, "Error al exportar");
        }
    }
}