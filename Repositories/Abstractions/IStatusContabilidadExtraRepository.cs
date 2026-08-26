#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

public interface IStatusContabilidadExtraRepository
{
    Task<IReadOnlyList<StatusContabilidadExtra>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// El extra del devengado (null si nunca se editó), con las mismas navegaciones
    /// que la carga completa. Alimenta la relectura de una fila tras guardar.
    /// </summary>
    Task<StatusContabilidadExtra?> GetByClaveAsync(string tipoDev, int nroDev, CancellationToken ct = default);
    Task UpsertAsync(StatusContabilidadExtra entity, CancellationToken ct = default);
}
