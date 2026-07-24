#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

public interface IDevengadoExtraRepository
{
    Task<DevengadoExtra?> GetByDevengadoIdAsync(int devengadoId, CancellationToken ct = default);
    Task<IReadOnlyList<DevengadoExtra>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Inserta o actualiza por DevengadoId (un registro editable por fila del ledger).</summary>
    Task UpsertAsync(DevengadoExtra entity, CancellationToken ct = default);
}
