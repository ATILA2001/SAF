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
        // detalle por OP las muestra a todas y el resumen exige unanimidad.
        var rows = await db.ExpedientesSeguro.AsNoTracking()
            .Where(s => distinct.Contains(s.Expediente))
            .Select(s => new
            {
                s.Expediente,
                s.Op,
                s.Estado,
                Seguro = s.SeguroOpcion == null ? null : s.SeguroOpcion.Nombre,
                s.Id,
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.Expediente, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    // Tanto el seguro como el estado se resumen solo por unanimidad: si las
                    // OPs difieren (una sin valor ya difiere) no se muestra ninguno — mostrar
                    // el de una sola sería informar el de todas — y es el detalle por OP el
                    // que enseña todos los casos. Nada de "peor caso gana".
                    var seguros = g
                        .Select(r => string.IsNullOrWhiteSpace(r.Seguro) ? null : r.Seguro.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var estados = g
                        .Select(r => string.IsNullOrWhiteSpace(r.Estado) ? null : r.Estado.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return new ResumenSeguroViewModel
                    {
                        Seguro = seguros.Count == 1 ? seguros[0] : null,
                        // Seguros mezclados = la celda queda vacía sin estar vacío el dato:
                        // el badge lo destaca para que el detalle no pase inadvertido.
                        SegurosMezclados = seguros.Count > 1,
                        Estado = estados.Count == 1 ? estados[0] : null,
                        Ops = g.Count(),
                        // Orden de carga (Id), como el resto de la tabla: por OP seria
                        // alfabetico y dejaria "23954/25" entre "224072/25" y "328090/25".
                        Lineas = g
                            .OrderBy(r => r.Id)
                            .Select(r => new LineaSeguroViewModel
                            {
                                Op = r.Op,
                                Seguro = string.IsNullOrWhiteSpace(r.Seguro) ? null : r.Seguro.Trim(),
                                Estado = string.IsNullOrWhiteSpace(r.Estado) ? null : r.Estado.Trim(),
                            })
                            .ToList(),
                    };
                },
                StringComparer.OrdinalIgnoreCase);
    }
}