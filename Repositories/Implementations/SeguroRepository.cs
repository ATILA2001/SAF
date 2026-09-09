#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.Common;
using SAF.Application.Seguros.Dtos;
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
            // Orden de carga (Id): los recién agregados quedan al final, como en la planilla.
            .OrderBy(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ExpedienteSeguro>> GetPageAsync(int skip, int take, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Mismo orden que GetAllAsync más desempate por Id: Skip/Take exige un orden
        // estrictamente determinístico o los lotes pueden repetir o saltear filas.
        return await db.ExpedientesSeguro.AsNoTracking()
            .Include(e => e.SeguroOpcion)
            // Mismo orden que GetAllAsync; el Id ya es único, Skip/Take es determinístico.
            .OrderBy(e => e.Id)
            .Skip(skip)
            .Take(take)
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
        var e = await db.ExpedientesSeguro.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return; // ya no existe: el resultado buscado

        // La confirmación del diálogo se basó en lo que el usuario tenía en pantalla:
        // el DELETE exige esa versión en el WHERE, así que si otro modificó la fila en
        // el medio se avisa en vez de borrar a ciegas. Sin versión no hay contra qué
        // comparar: mismo criterio fail-closed que los upserts.
        if (rowVersion is null)
            throw new ConflictoDeConcurrenciaException();
        db.Entry(e).Property(x => x.RowVersion).OriginalValue = rowVersion;

        db.ExpedientesSeguro.Remove(e);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoDeConcurrenciaException();
        }
    }

    public async Task<IReadOnlyDictionary<string, ResumenSeguroViewModel>> GetResumenSeguroByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default)
    {
        var empty = new Dictionary<string, ResumenSeguroViewModel>(StringComparer.OrdinalIgnoreCase);
        if (expedientes is null || expedientes.Count == 0)
            return empty;

        var distinct = expedientes
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
            return empty;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Se traen TODAS las filas del expediente, tengan o no seguro asignado: el
        // estado y el detalle por OP existen igual sin seguro; solo el resumen de
        // seguro ignora las filas sin él (abajo).
        var rows = await db.ExpedientesSeguro.AsNoTracking()
            .Where(s => distinct.Contains(s.Expediente))
            .Select(s => new
            {
                s.Expediente,
                s.Op,
                s.ImporteNeto,
                s.Estado,
                Seguro = s.SeguroOpcion == null ? null : s.SeguroOpcion.Nombre,
                EsOk = s.SeguroOpcion == null ? (bool?)null : s.SeguroOpcion.EsOk,
                s.FechaModificacion,
                s.Id,
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.Expediente, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    // Peor caso gana: si alguna fila del expediente NO está ok, se muestra esa
                    // (la columna existe para frenar pagos con seguro pendiente). Desempate
                    // determinista: modificación más reciente y luego Id.
                    var seguro = g.Where(r => r.EsOk != null)
                        .OrderBy(r => r.EsOk)                     // false (no ok) primero
                        .ThenByDescending(r => r.FechaModificacion)
                        .ThenByDescending(r => r.Id)
                        .FirstOrDefault()?.Seguro;

                    // El estado es texto libre (no lista): no hay "peor caso" posible.
                    // Se informa solo si todas las OPs coinciden (sin estado cuenta como
                    // distinto); si difieren, la celda queda vacía y el detalle lo muestra.
                    var estados = g
                        .Select(r => string.IsNullOrWhiteSpace(r.Estado) ? null : r.Estado.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return new ResumenSeguroViewModel
                    {
                        Seguro = seguro,
                        Estado = estados.Count == 1 ? estados[0] : null,
                        Ops = g.Count(),
                        Lineas = g
                            .OrderBy(r => r.Op)
                            .ThenBy(r => r.Id)
                            .Select(r => new LineaSeguroViewModel
                            {
                                Op = r.Op,
                                ImporteNeto = r.ImporteNeto,
                                Seguro = r.Seguro,
                                Estado = string.IsNullOrWhiteSpace(r.Estado) ? null : r.Estado.Trim(),
                            })
                            .ToList(),
                    };
                },
                StringComparer.OrdinalIgnoreCase);
    }
}