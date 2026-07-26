#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.Caf.Dtos;
using SAF.Application.Common;
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

    public async Task<IReadOnlyList<ExpedienteCaf>> GetPageAsync(int skip, int take, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Mismo orden que GetAllAsync más desempate por Id: Skip/Take exige un orden
        // estrictamente determinístico o los lotes pueden repetir o saltear filas.
        return await db.ExpedientesCaf.AsNoTracking()
            .OrderByDescending(e => e.Anio)
            .ThenBy(e => e.Expediente)
            .ThenBy(e => e.Op)
            .ThenBy(e => e.Id)
            .Skip(skip)
            .Take(take)
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

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // El WHERE incluye la RowVersion que traía la fila al editarse: cero filas
            // afectadas significa que otro usuario la modificó o la borró.
            throw new ConflictoDeConcurrenciaException();
        }
    }

    public async Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.ExpedientesCaf.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return; // ya no existe: el resultado buscado

        // La confirmación del diálogo se basó en lo que el usuario tenía en pantalla:
        // el DELETE exige esa versión en el WHERE, así que si otro modificó la fila en
        // el medio se avisa en vez de borrar a ciegas.
        if (rowVersion is not null)
            db.Entry(e).Property(x => x.RowVersion).OriginalValue = rowVersion;

        db.ExpedientesCaf.Remove(e);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoDeConcurrenciaException();
        }
    }

    public async Task<IReadOnlyDictionary<string, ResumenCafViewModel>> GetResumenCafByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default)
    {
        var empty = new Dictionary<string, ResumenCafViewModel>(StringComparer.OrdinalIgnoreCase);
        if (expedientes is null || expedientes.Count == 0)
            return empty;

        var distinct = expedientes
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
            return empty;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Se cuentan TODAS las OPs del expediente, no solo las pagadas: las impagas son
        // justamente el dato que decide si la fecha representa un pago completo.
        var rows = await db.ExpedientesCaf.AsNoTracking()
            .Where(c => distinct.Contains(c.Expediente))
            .Select(c => new { c.Expediente, c.Op, c.ImporteNeto, c.FechaPago })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.Expediente, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new ResumenCafViewModel
                {
                    UltimoPago = g.Max(x => x.FechaPago),
                    Ops = g.Count(),
                    OpsPagadas = g.Count(x => x.FechaPago != null),
                    // Pendientes primero: son las que explican por qué no hay fecha.
                    Lineas = g
                        .OrderBy(x => x.FechaPago.HasValue)
                        .ThenBy(x => x.FechaPago)
                        .ThenBy(x => x.Op)
                        .Select(x => new LineaCafViewModel
                        {
                            Op = x.Op,
                            ImporteNeto = x.ImporteNeto,
                            FechaPago = x.FechaPago,
                        })
                        .ToList(),
                },
                StringComparer.OrdinalIgnoreCase);
    }

}