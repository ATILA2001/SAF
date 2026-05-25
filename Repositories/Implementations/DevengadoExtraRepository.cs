#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class DevengadoExtraRepository(AppDbContext db) : IDevengadoExtraRepository
{
    public async Task<DevengadoExtra?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default)
        => await db.DevengadosExtra
            .Include(e => e.StatusDgayfOpcion)
            .Include(e => e.StatusOpOpcion)
            .FirstOrDefaultAsync(e => e.TipoDev == tipoDev && e.NroDev == nroDev, ct);

    public async Task<IReadOnlyList<DevengadoExtra>> GetAllAsync(CancellationToken ct = default)
        => await db.DevengadosExtra
            .Include(e => e.StatusDgayfOpcion)
            .Include(e => e.StatusOpOpcion)
            .OrderBy(e => e.TipoDev)
            .ThenBy(e => e.NroDev)
            .ToListAsync(ct);

    public async Task UpsertAsync(DevengadoExtra entity, CancellationToken ct = default)
    {
        var existing = await db.DevengadosExtra
            .FirstOrDefaultAsync(e => e.TipoDev == entity.TipoDev && e.NroDev == entity.NroDev, ct);

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
            existing.StatusContable = entity.StatusContable;
            existing.SegurosTeso = entity.SegurosTeso;
            existing.FechaDePagoNoCaf = entity.FechaDePagoNoCaf;
            existing.FechaDePagoCaf = entity.FechaDePagoCaf;
            existing.FechaPagoTotal = entity.FechaPagoTotal;
            existing.FechaSade = entity.FechaSade;
            existing.BuzonSade = entity.BuzonSade;
            existing.PedidoFactura2 = entity.PedidoFactura2;
            existing.PedidoFactura3 = entity.PedidoFactura3;
            existing.FechaFacturaCorrecta = entity.FechaFacturaCorrecta;
            existing.CafSiNo = entity.CafSiNo;
            existing.FechaModificacion = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
