#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class CafRepository(IDbContextFactory<AppDbContext> dbFactory) : ICafRepository
{
    public async Task<IReadOnlyList<ExpedienteCaf>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ExpedientesCaf.AsNoTracking()
            .OrderByDescending(e => e.Anio)
            .ThenBy(e => e.Expediente)
            .ThenBy(e => e.Op)
            .ToListAsync(ct);
    }

    public async Task<ExpedienteCaf?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ExpedientesCaf.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task AddAsync(ExpedienteCaf entity, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ExpedientesCaf.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ExpedienteCaf entity, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ExpedientesCaf.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.ExpedientesCaf.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return;
        db.ExpedientesCaf.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, DateTime>> GetFechaPagoCafByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default)
    {
        var empty = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        if (expedientes is null || expedientes.Count == 0)
            return empty;

        var distinct = expedientes
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
            return empty;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var rows = await db.ExpedientesCaf.AsNoTracking()
            .Where(c => c.FechaPago != null
                     && distinct.Contains(c.Expediente))
            .GroupBy(c => c.Expediente)
            .Select(g => new { Expediente = g.Key, FechaPago = g.Max(x => x.FechaPago) })
            .ToListAsync(ct);

        var dict = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
            if (r.FechaPago is DateTime f)
                dict[r.Expediente] = f;
        return dict;
    }
}