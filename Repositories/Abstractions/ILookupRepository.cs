#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

public interface ILookupRepository
{
    Task<IReadOnlyList<StatusDgayfOpcion>> GetStatusDgayfOpcionesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StatusOpOpcion>> GetStatusOpOpcionesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StatusContableOpcion>> GetStatusContableOpcionesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TramitadorCuentasPagarOpcion>> GetTramitadoresCuentasPagarAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TramitadorLiquidacionesOpcion>> GetTramitadoresLiquidacionesAsync(CancellationToken ct = default);
}
