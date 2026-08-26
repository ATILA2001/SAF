#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

public interface IDevengadoExtraRepository
{
    Task<IReadOnlyList<DevengadoExtra>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Los extras de esas filas del ledger, con las mismas navegaciones que la carga
    /// completa. Alimenta la relectura de una fila tras guardar.
    /// </summary>
    Task<IReadOnlyList<DevengadoExtra>> GetByDevengadoIdsAsync(
        IReadOnlyCollection<int> devengadoIds, CancellationToken ct = default);

    /// <summary>Inserta o actualiza por DevengadoId (un registro editable por fila del ledger).</summary>
    Task UpsertAsync(DevengadoExtra entity, CancellationToken ct = default);
}
