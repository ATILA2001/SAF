#nullable enable
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class StatusContabilidadExtraRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ExtraRepositoryBase<StatusContabilidadExtra>(dbFactory), IStatusContabilidadExtraRepository
{
    // Única definición de la consulta de lectura (navegaciones incluidas): la
    // comparten la carga completa y la búsqueda por clave, para que un Include
    // agregado a una no pueda faltar en la otra.
    private static IQueryable<StatusContabilidadExtra> Query(AppDbContext db) =>
        db.StatusContabilidadExtras.AsNoTracking()
            .Include(e => e.StatusContableOpcion)
            .Include(e => e.TramitadorCuentasPagarOpcion)
            .Include(e => e.TramitadorLiquidacionesOpcion);

    public async Task<IReadOnlyList<StatusContabilidadExtra>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(ct);
        return await Query(db)
            .OrderBy(e => e.TipoDev)
            .ThenBy(e => e.NroDev)
            .ToListAsync(ct);
    }

    public async Task<StatusContabilidadExtra?> GetByClaveAsync(
        string tipoDev, int nroDev, CancellationToken ct = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(ct);
        return await Query(db)
            .FirstOrDefaultAsync(e => e.TipoDev == tipoDev && e.NroDev == nroDev, ct);
    }

    protected override Expression<Func<StatusContabilidadExtra, bool>> MismaClave(StatusContabilidadExtra entity)
        => e => e.TipoDev == entity.TipoDev && e.NroDev == entity.NroDev;

    protected override void CopiarCampos(StatusContabilidadExtra origen, StatusContabilidadExtra destino)
    {
        destino.FechaPedidoFactura2 = origen.FechaPedidoFactura2;
        destino.ReiterarPedidoFactura3 = origen.ReiterarPedidoFactura3;
        destino.FechaRechazo = origen.FechaRechazo;
        destino.FechaIngresoFactura = origen.FechaIngresoFactura;
        destino.SinFacturaMotivo = origen.SinFacturaMotivo;
        destino.StatusContableOpcionId = origen.StatusContableOpcionId;
        destino.ObservacionesCuentasPagar = origen.ObservacionesCuentasPagar;
        destino.TramitadorCuentasPagarOpcionId = origen.TramitadorCuentasPagarOpcionId;
        destino.TramitadorLiquidacionesOpcionId = origen.TramitadorLiquidacionesOpcionId;
        destino.ObservacionesLiquidaciones = origen.ObservacionesLiquidaciones;
    }
}
