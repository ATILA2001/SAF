#nullable enable
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;

namespace SAF.Services.Implementations;

public class LookupService(ILookupRepository repo) : ILookupService
{
    public Task<IReadOnlyList<StatusDgayfOpcion>> GetStatusDgayfOpcionesAsync(CancellationToken ct = default)
        => repo.GetStatusDgayfOpcionesAsync(ct);

    public Task<IReadOnlyList<StatusOpOpcion>> GetStatusOpOpcionesAsync(CancellationToken ct = default)
        => repo.GetStatusOpOpcionesAsync(ct);

    public Task<IReadOnlyList<StatusContableOpcion>> GetStatusContableOpcionesAsync(CancellationToken ct = default)
        => repo.GetStatusContableOpcionesAsync(ct);

    public Task<IReadOnlyList<TramitadorCuentasPagarOpcion>> GetTramitadoresCuentasPagarAsync(CancellationToken ct = default)
        => repo.GetTramitadoresCuentasPagarAsync(ct);

    public Task<IReadOnlyList<TramitadorLiquidacionesOpcion>> GetTramitadoresLiquidacionesAsync(CancellationToken ct = default)
        => repo.GetTramitadoresLiquidacionesAsync(ct);
}
