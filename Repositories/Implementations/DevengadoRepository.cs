#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class DevengadoRepository(IDbContextFactory<AppDbContext> dbFactory) : IDevengadoRepository
{
    public async Task<IReadOnlyList<Devengado>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Devengados.AsNoTracking()
            .OrderBy(d => d.TipoDev)
            .ThenBy(d => d.NroDev)
            .ToListAsync(ct);
    }

    public async Task<Devengado?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Devengados.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<Devengado?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Devengados.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TipoDev == tipoDev && d.NroDev == nroDev, ct);
    }

    public async Task<decimal> GetSumImportePpByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Devengados.AsNoTracking()
            .Where(d => d.TipoDev == tipoDev && d.NroDev == nroDev)
            .SumAsync(d => d.ImportePp ?? 0m, ct);
    }

    public async Task<DateTime?> GetMaxFechaImputacionAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Devengados.AsNoTracking()
            .Select(d => d.FechaImputacion)
            .MaxAsync(ct);
    }

    public async Task AddAsync(Devengado entity, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Devengados.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.Devengados.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (e is null) return;
        db.Devengados.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsExactoAsync(string tipoDev, int nroDev, DateTime? fechaImputacion,
        decimal? importePp, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Devengados.AsNoTracking().AnyAsync(d =>
            d.TipoDev == tipoDev && d.NroDev == nroDev
            && d.FechaImputacion == fechaImputacion && d.ImportePp == importePp, ct);
    }
}