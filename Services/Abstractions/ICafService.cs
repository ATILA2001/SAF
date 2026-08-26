#nullable enable
using SAF.Application.Caf.Dtos;

namespace SAF.Services.Abstractions;

public interface ICafService
{
    Task<IReadOnlyList<CafViewModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Relee una sola fila, para refrescar en el lugar la que se acaba de guardar.
    /// Null: otro usuario la borró mientras se editaba.
    /// </summary>
    Task<CafViewModel?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CafViewModel> CreateAsync(CafViewModel vm, CancellationToken ct = default);
    Task UpdateAsync(CafViewModel vm, CancellationToken ct = default);
    /// <summary>Página de la vista con orden determinístico, para la carga progresiva.</summary>
    Task<IReadOnlyList<CafViewModel>> GetPageAsync(int skip, int take, CancellationToken ct = default);

    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
}
