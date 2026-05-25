#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class StatusContabilidadExtraRepository(AppDbContext db) : IStatusContabilidadExtraRepository
{
    public async Task<StatusContabilidadExtra?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default)
        => await db.StatusContabilidadExtras
            .Include(e => e.StatusContableOpcion)
            .Include(e => e.TramitadorCuentasPagarOpcion)
            .Include(e => e.TramitadorLiquidacionesOpcion)
            .FirstOrDefaultAsync(e => e.TipoDev == tipoDev && e.NroDev == nroDev, ct);

    public async Task<IReadOnlyList<StatusContabilidadExtra>> GetAllAsync(CancellationToken ct = default)
        => await db.StatusContabilidadExtras
            .Include(e => e.StatusContableOpcion)
            .Include(e => e.TramitadorCuentasPagarOpcion)
            .Include(e => e.TramitadorLiquidacionesOpcion)
            .OrderBy(e => e.TipoDev)
            .ThenBy(e => e.NroDev)
            .ToListAsync(ct);

    public async Task UpsertAsync(StatusContabilidadExtra entity, CancellationToken ct = default)
    {
        var existing = await db.StatusContabilidadExtras
            .FirstOrDefaultAsync(e => e.TipoDev == entity.TipoDev && e.NroDev == entity.NroDev, ct);

        if (existing is null)
        {
            entity.FechaCreacion = DateTime.UtcNow;
            entity.FechaModificacion = DateTime.UtcNow;
            db.StatusContabilidadExtras.Add(entity);
        }
        else
        {
            existing.FechaPedidoFactura2 = entity.FechaPedidoFactura2;
            existing.ReiterarPedidoFactura3 = entity.ReiterarPedidoFactura3;
            existing.FechaIngresoFactura = entity.FechaIngresoFactura;
            existing.StatusContableOpcionId = entity.StatusContableOpcionId;
            existing.ObservacionesCuentasPagar = entity.ObservacionesCuentasPagar;
            existing.TramitadorCuentasPagarOpcionId = entity.TramitadorCuentasPagarOpcionId;
            existing.TramitadorLiquidacionesOpcionId = entity.TramitadorLiquidacionesOpcionId;
            existing.ObservacionesLiquidaciones = entity.ObservacionesLiquidaciones;
            existing.FaltaPoliza = entity.FaltaPoliza;
            existing.UltimoMovimientoSade = entity.UltimoMovimientoSade;
            existing.FechaModificacion = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
