#nullable enable
using SAF.Application.Caf.Dtos;

namespace SAF.Services.Abstractions;

public interface ICafService
{
    Task<IReadOnlyList<CafViewModel>> GetAllAsync(CancellationToken ct = default);
    Task<CafViewModel> CreateAsync(CafViewModel vm, CancellationToken ct = default);
    Task UpdateAsync(CafViewModel vm, CancellationToken ct = default);
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
}
