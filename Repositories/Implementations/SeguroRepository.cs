#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class SeguroRepository(IDbContextFactory<AppDbContext> dbFactory) : ISeguroRepository
{
    public async Task<IReadOnlyList<ExpedienteSeguro>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ExpedientesSeguro.AsNoTracking()
            .Include(e => e.SeguroOpcion)
            .OrderBy(e => e.Expediente)
            .ThenBy(e => e.Op)
            .ToListAsync(ct);
    }

    public async Task<ExpedienteSeguro?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ExpedientesSeguro.AsNoTracking()
            .Include(e => e.SeguroOpcion)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task AddAsync(ExpedienteSeguro entity, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ExpedientesSeguro.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ExpedienteSeguro entity, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Solo la fila: la opción de seguro navegada viene de una lectura previa y no
        // debe reinsertarse ni modificarse al guardar.
        db.Entry(entity).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.ExpedientesSeguro.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return;
        db.ExpedientesSeguro.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetSeguroByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default)
    {
        var empty = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (expedientes is null || expedientes.Count == 0)
            return empty;

        var distinct = expedientes
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
            return empty;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var rows = await db.ExpedientesSeguro.AsNoTracking()
            .Where(s => s.SeguroOpcionId != null
                     && distinct.Contains(s.Expediente))
            .Select(s => new
            {
                s.Expediente,
                s.SeguroOpcion!.Nombre,
                s.SeguroOpcion.EsOk,
                s.FechaModificacion,
                s.Id,
            })
            .ToListAsync(ct);

        // Peor caso gana: si alguna fila del expediente NO está ok, se muestra esa
        // (la columna existe para frenar pagos con seguro pendiente). Desempate
        // determinista: modificación más reciente y luego Id.
        return rows
            .GroupBy(r => r.Expediente, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.EsOk)                       // false (no ok) primero
                      .ThenByDescending(r => r.FechaModificacion)
                      .ThenByDescending(r => r.Id)
                      .First().Nombre,
                StringComparer.OrdinalIgnoreCase);
    }
}