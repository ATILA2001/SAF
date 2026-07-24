#nullable enable
using SAF.Application.Pagos.Dtos;

namespace SAF.Services.Abstractions;

public interface IPagosService
{
    Task<IReadOnlyList<PagoViewModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Una fila del ledger por su Id (cada fila se edita por separado).</summary>
    Task<PagoViewModel?> GetByIdAsync(int devengadoId, CancellationToken ct = default);

    Task UpsertAsync(PagoViewModel vm, CancellationToken ct = default);

    /// <summary>
    /// Alta manual de un devengado en el ledger (complementa la sincronización con IVC,
    /// p. ej. para días que quedaron sin sincronizar). Valida y normaliza los datos.
    /// </summary>
    Task<PagoViewModel> CreateDevengadoAsync(PagoViewModel vm, CancellationToken ct = default);

    /// <summary>Elimina una fila del ledger y sus datos editables asociados.</summary>
    Task DeleteDevengadoAsync(int devengadoId, CancellationToken ct = default);

    /// <summary>
    /// Última FECHA_IMPUTACION sincronizada (máxima de la tabla acumulativa).
    /// Null si aún no se importó ningún devengado.
    /// </summary>
    Task<DateTime?> GetUltimaFechaImputacionAsync(CancellationToken ct = default);
}
