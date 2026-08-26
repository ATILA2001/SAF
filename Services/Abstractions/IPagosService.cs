#nullable enable
using SAF.Application.Pagos.Dtos;

namespace SAF.Services.Abstractions;

public interface IPagosService
{
    /// <summary>Fase propia: todas las columnas salvo las derivadas de IVC.</summary>
    Task<IReadOnlyList<PagoViewModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Página de la fase propia con orden determinístico, para la carga progresiva.</summary>
    Task<IReadOnlyList<PagoViewModel>> GetPageAsync(int skip, int take, CancellationToken ct = default);

    /// <summary>
    /// Relee una sola fila con sus derivadas propias, para refrescar en el lugar la que
    /// se acaba de guardar. Null: otro usuario la borró mientras se editaba.
    /// </summary>
    Task<PagoViewModel?> GetByIdAsync(int devengadoId, CancellationToken ct = default);

    /// <summary>
    /// Rellena sobre los ítems ya pintados las columnas que dependen de IVC (Fecha y
    /// Buzón SADE, Fecha Pago No CAF y Fecha Pago Total). Se difiere porque la primera
    /// conexión a IVC puede tardar segundos y no debe frenar la primera pintada.
    /// </summary>
    Task CompletarIvcAsync(IReadOnlyList<PagoViewModel> items, CancellationToken ct = default);

    Task UpsertAsync(PagoViewModel vm, CancellationToken ct = default);

    /// <summary>Detecta una fila idéntica antes del alta; la vista decide si confirmar.</summary>
    Task<bool> ExisteDevengadoIdenticoAsync(PagoViewModel vm, CancellationToken ct = default);

    /// <summary>
    /// Alta manual de un devengado en el ledger (complementa la sincronización con IVC,
    /// p. ej. para días que quedaron sin sincronizar). Valida y normaliza los datos.
    /// </summary>
    Task<PagoViewModel> CreateDevengadoAsync(PagoViewModel vm, CancellationToken ct = default);

    /// <summary>
    /// Elimina una fila del ledger y sus datos editables asociados. La versión del extra
    /// que traía la grilla detecta datos cargados/modificados por otro usuario en el medio.
    /// </summary>
    Task DeleteDevengadoAsync(int devengadoId, byte[]? extraRowVersion, CancellationToken ct = default);

    /// <summary>
    /// Última FECHA_IMPUTACION sincronizada (máxima de la tabla acumulativa).
    /// Null si aún no se importó ningún devengado.
    /// </summary>
    Task<DateTime?> GetUltimaFechaImputacionAsync(CancellationToken ct = default);
}
