#nullable enable
using SAF.ViewModels.StatusContabilidad;

namespace SAF.Services.Abstractions;

public interface IStatusContabilidadService
{
    Task<IReadOnlyList<StatusContabilidadViewModel>> GetAllAsync(CancellationToken ct = default);
    Task<StatusContabilidadViewModel?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default);
    Task UpsertAsync(StatusContabilidadViewModel vm, CancellationToken ct = default);
}
