#nullable enable
using Microsoft.EntityFrameworkCore;
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