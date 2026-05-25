#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

public interface IDevengadoExtraRepository
{
    Task<DevengadoExtra?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default);
    Task<IReadOnlyList<DevengadoExtra>> GetAllAsync(CancellationToken ct = default);
    Task UpsertAsync(DevengadoExtra entity, CancellationToken ct = default);
}
