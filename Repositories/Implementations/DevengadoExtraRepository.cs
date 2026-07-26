#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.Common;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class DevengadoExtraRepository(IDbContextFactory<AppDbContext> dbFactory) : IDevengadoExtraRepository
{
    public async Task<DevengadoExtra?> GetByDevengadoIdAsync(int devengadoId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.DevengadosExtra.AsNoTracking()
            .Include(e => e.StatusDgayfOpcion)
            .Include(e => e.StatusOpOpcion)
            .FirstOrDefaultAsync(e => e.DevengadoId == devengadoId, ct);
    }

    public async Task<IReadOnlyList<DevengadoExtra>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.DevengadosExtra.AsNoTracking()
            .Include(e => e.StatusDgayfOpcion)
            .Include(e => e.StatusOpOpcion)
            .OrderBy(e => e.TipoDev)
            .ThenBy(e => e.NroDev)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(DevengadoExtra entity, CancellationToken ct = default)
    {
        try
        {
            await GuardarAsync(entity, ct);
        }
        catch (DbUpdateException ex) when (ErroresSql.EsClaveDuplicada(ex))
        {
            // Otro usuario insertó la fila entre el chequeo y el guardado: el usuario
            // editó sobre datos que ya no existen, así que actualizar acá pisaría lo
            // recién guardado sin aviso. Es un conflicto, no un caso de reintento.
            throw new ConflictoDeConcurrenciaException();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoDeConcurrenciaException();
        }
    }

    private async Task GuardarAsync(DevengadoExtra entity, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.DevengadosExtra
            .FirstOrDefaultAsync(e => e.DevengadoId == entity.DevengadoId, ct);

        if (existing is null)
        {
            entity.FechaCreacion = DateTime.UtcNow;
            entity.FechaModificacion = DateTime.UtcNow;
            db.DevengadosExtra.Add(entity);
        }
        else
        {
            // Sin versión significa que al cargar la grilla el extra no existía y otro
            // usuario lo creó en el medio: actualizar pisaría sus datos sin aviso.
            if (entity.RowVersion is null)
                throw new ConflictoDeConcurrenciaException();

            // Se compara contra la versión que el usuario tenía cargada, no contra la
            // recién leída: si otro guardó en el medio, el UPDATE no afecta filas.
            db.Entry(existing).Property(e => e.RowVersion).OriginalValue = entity.RowVersion;

            existing.StatusDgayfOpcionId = entity.StatusDgayfOpcionId;
            existing.StatusOpOpcionId = entity.StatusOpOpcionId;
            existing.FechaFirmaOp = entity.FechaFirmaOp;
            existing.Observaciones = entity.Observaciones;
            existing.Ccoo = entity.Ccoo;
            existing.FechaCcoo = entity.FechaCcoo;
            existing.FechaNotificacion = entity.FechaNotificacion;
            existing.CafSiNo = entity.CafSiNo;
            existing.FechaModificacion = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}