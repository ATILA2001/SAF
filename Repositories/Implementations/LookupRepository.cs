#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class LookupRepository(AppDbContext db) : ILookupRepository
{
    public async Task<IReadOnlyList<StatusDgayfOpcion>> GetStatusDgayfOpcionesAsync(CancellationToken ct = default)
        => await db.StatusDgayfOpciones.Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);

    public async Task<IReadOnlyList<StatusOpOpcion>> GetStatusOpOpcionesAsync(CancellationToken ct = default)
        => await db.StatusOpOpciones.Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);

    public async Task<IReadOnlyList<StatusContableOpcion>> GetStatusContableOpcionesAsync(CancellationToken ct = default)
        => await db.StatusContableOpciones.Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);

    public async Task<IReadOnlyList<TramitadorCuentasPagarOpcion>> GetTramitadoresCuentasPagarAsync(CancellationToken ct = default)
        => await db.TramitadoresCuentasPagar.Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);

    public async Task<IReadOnlyList<TramitadorLiquidacionesOpcion>> GetTramitadoresLiquidacionesAsync(CancellationToken ct = default)
        => await db.TramitadoresLiquidaciones.Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);
}
