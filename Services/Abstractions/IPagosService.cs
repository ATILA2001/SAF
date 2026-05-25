#nullable enable
using SAF.ViewModels.Pagos;

namespace SAF.Services.Abstractions;

public interface IPagosService
{
    Task<IReadOnlyList<PagoViewModel>> GetAllAsync(CancellationToken ct = default);
    Task<PagoViewModel?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default);
    Task UpsertAsync(PagoViewModel vm, CancellationToken ct = default);
}
