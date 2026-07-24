#nullable enable
using SAF.Application.Seguros.Dtos;

namespace SAF.Services.Abstractions;

public interface ISeguroService
{
    Task<IReadOnlyList<SeguroViewModel>> GetAllAsync(CancellationToken ct = default);
    Task<SeguroViewModel> CreateAsync(SeguroViewModel vm, CancellationToken ct = default);
    Task UpdateAsync(SeguroViewModel vm, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
