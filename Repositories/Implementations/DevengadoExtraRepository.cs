#nullable enable
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class DevengadoExtraRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ExtraRepositoryBase<DevengadoExtra>(dbFactory), IDevengadoExtraRepository
{
    public async Task<IReadOnlyList<DevengadoExtra>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(ct);
        return await db.DevengadosExtra.AsNoTracking()
            .Include(e => e.StatusDgayfOpcion)
            .Include(e => e.StatusOpOpcion)
            .OrderBy(e => e.TipoDev)
            .ThenBy(e => e.NroDev)
            .ToListAsync(ct);
    }

    protected override Expression<Func<DevengadoExtra, bool>> MismaClave(DevengadoExtra entity)
        => e => e.DevengadoId == entity.DevengadoId;

    protected override void CopiarCampos(DevengadoExtra origen, DevengadoExtra destino)
    {
        destino.StatusDgayfOpcionId = origen.StatusDgayfOpcionId;
        destino.StatusOpOpcionId = origen.StatusOpOpcionId;
        destino.FechaFirmaOp = origen.FechaFirmaOp;
        destino.Observaciones = origen.Observaciones;
        destino.Ccoo = origen.Ccoo;
        destino.FechaCcoo = origen.FechaCcoo;
        destino.FechaNotificacion = origen.FechaNotificacion;
        destino.CafSiNo = origen.CafSiNo;
    }
}
