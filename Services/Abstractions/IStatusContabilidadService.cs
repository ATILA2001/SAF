#nullable enable
using SAF.Application.StatusContabilidad.Dtos;

namespace SAF.Services.Abstractions;

public interface IStatusContabilidadService
{
    Task<IReadOnlyList<StatusContabilidadViewModel>> GetAllAsync(CancellationToken ct = default);
    Task UpsertAsync(StatusContabilidadViewModel vm, CancellationToken ct = default);
}
