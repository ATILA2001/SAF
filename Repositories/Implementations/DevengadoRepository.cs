#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.Common;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class DevengadoRepository(IDbContextFactory<AppDbContext> dbFactory) : IDevengadoRepository
{
    // Orden cronológico como la planilla: fechas más viejas arriba, lo reciente al
    // final (FechaImputacion es la "Fecha Devengado" de la grilla). Alimenta Pagos
    // y el tablero de Status (que agrupa preservando este orden). El desempate por
    // tipo y número mantiene juntas las líneas de un devengado con la misma fecha.
    public async Task<IReadOnlyList<Devengado>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Devengados.AsNoTracking()
            .OrderBy(d => d.FechaImputacion)
            .ThenBy(d => d.TipoDev)
            .ThenBy(d => d.NroDev)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Devengado>> GetPageAsync(int skip, int take, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Mismo orden que GetAllAsync más desempate por Id: Skip/Take exige un orden
        // estrictamente determinístico o los lotes pueden repetir o saltear filas.
        return await db.Devengados.AsNoTracking()
            .OrderBy(d => d.FechaImputacion)
            .ThenBy(d => d.TipoDev)
            .ThenBy(d => d.NroDev)
            .ThenBy(d => d.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
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

    public async Task DeleteAsync(int id, byte[]? extraRowVersion, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.Devengados.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (e is null) return; // ya no existe: el resultado buscado

        // El devengado no tiene rowversion propia, pero el borrado arrastra su extra:
        // si el extra cambió (o apareció) desde que el usuario cargó la grilla,
        // borrarlo se llevaría datos que otro acaba de cargar o modificar.
        var extra = await db.DevengadosExtra.FirstOrDefaultAsync(x => x.DevengadoId == id, ct);
        if (extraRowVersion is null)
        {
            // El usuario tenía la fila sin extra: si apareció uno en el medio, conflicto.
            // (Ventana check-then-act residual solo en este caso; cerrarla exigiría un
            // lock explícito y no lo vale.)
            if (extra is not null)
                throw new ConflictoDeConcurrenciaException();
        }
        else
        {
            if (extra is null)
                throw new ConflictoDeConcurrenciaException();

            // El DELETE del extra exige la versión en el WHERE (atómico, mismo patrón
            // que CAF/Seguros): si otro lo modificó en el medio, conflicto.
            db.Entry(extra).Property(x => x.RowVersion).OriginalValue = extraRowVersion;
            db.DevengadosExtra.Remove(extra);
        }

        db.Devengados.Remove(e);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoDeConcurrenciaException();
        }
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