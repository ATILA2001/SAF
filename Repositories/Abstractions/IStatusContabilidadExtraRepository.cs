#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

public interface IStatusContabilidadExtraRepository
{
    Task<IReadOnlyList<StatusContabilidadExtra>> GetAllAsync(CancellationToken ct = default);
    Task UpsertAsync(StatusContabilidadExtra entity, CancellationToken ct = default);
}
