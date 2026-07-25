#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class LookupRepository(IDbContextFactory<AppDbContext> dbFactory) : ILookupRepository
{
    public async Task<IReadOnlyList<StatusDgayfOpcion>> GetStatusDgayfOpcionesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.StatusDgayfOpciones.AsNoTracking()
            .Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StatusOpOpcion>> GetStatusOpOpcionesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.StatusOpOpciones.AsNoTracking()
            .Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StatusContableOpcion>> GetStatusContableOpcionesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.StatusContableOpciones.AsNoTracking()
            .Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TramitadorCuentasPagarOpcion>> GetTramitadoresCuentasPagarAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.TramitadoresCuentasPagar.AsNoTracking()
            .Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TramitadorLiquidacionesOpcion>> GetTramitadoresLiquidacionesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.TramitadoresLiquidaciones.AsNoTracking()
            .Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SeguroOpcion>> GetSeguroOpcionesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.SeguroOpciones.AsNoTracking()
            .Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync(ct);
    }
}