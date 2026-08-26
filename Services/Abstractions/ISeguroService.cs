#nullable enable
using SAF.Application.Seguros.Dtos;

namespace SAF.Services.Abstractions;

public interface ISeguroService
{
    Task<IReadOnlyList<SeguroViewModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Relee una sola fila, para refrescar en el lugar la que se acaba de guardar.
    /// Null: otro usuario la borró mientras se editaba.
    /// </summary>
    Task<SeguroViewModel?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SeguroViewModel> CreateAsync(SeguroViewModel vm, CancellationToken ct = default);
    Task UpdateAsync(SeguroViewModel vm, CancellationToken ct = default);
    /// <summary>Página de la vista con orden determinístico, para la carga progresiva.</summary>
    Task<IReadOnlyList<SeguroViewModel>> GetPageAsync(int skip, int take, CancellationToken ct = default);

    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
}
