#nullable enable
using SAF.Application.Pagos.Dtos;

namespace SAF.Services.Abstractions;

public interface IPagosService
{
    Task<IReadOnlyList<PagoViewModel>> GetAllAsync(CancellationToken ct = default);

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
