#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.Common;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class StatusContabilidadExtraRepository(IDbContextFactory<AppDbContext> dbFactory) : IStatusContabilidadExtraRepository
{
    public async Task<StatusContabilidadExtra?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.StatusContabilidadExtras.AsNoTracking()
            .Include(e => e.StatusContableOpcion)
            .Include(e => e.TramitadorCuentasPagarOpcion)
            .Include(e => e.TramitadorLiquidacionesOpcion)
            .FirstOrDefaultAsync(e => e.TipoDev == tipoDev && e.NroDev == nroDev, ct);
    }

    public async Task<IReadOnlyList<StatusContabilidadExtra>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.StatusContabilidadExtras.AsNoTracking()
            .Include(e => e.StatusContableOpcion)
            .Include(e => e.TramitadorCuentasPagarOpcion)
            .Include(e => e.TramitadorLiquidacionesOpcion)
            .OrderBy(e => e.TipoDev)
            .ThenBy(e => e.NroDev)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(StatusContabilidadExtra entity, CancellationToken ct = default)
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

    private async Task GuardarAsync(StatusContabilidadExtra entity, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

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
            // Sin versión significa que al cargar la grilla el extra no existía y otro
            // usuario lo creó en el medio: actualizar pisaría sus datos sin aviso.
            if (entity.RowVersion is null)
                throw new ConflictoDeConcurrenciaException();

            // Se compara contra la versión que el usuario tenía cargada, no contra la
            // recién leída: si otro guardó en el medio, el UPDATE no afecta filas.
            db.Entry(existing).Property(e => e.RowVersion).OriginalValue = entity.RowVersion;

            existing.FechaPedidoFactura2 = entity.FechaPedidoFactura2;
            existing.ReiterarPedidoFactura3 = entity.ReiterarPedidoFactura3;
            existing.FechaIngresoFactura = entity.FechaIngresoFactura;
            existing.SinFacturaMotivo = entity.SinFacturaMotivo;
            existing.StatusContableOpcionId = entity.StatusContableOpcionId;
            existing.ObservacionesCuentasPagar = entity.ObservacionesCuentasPagar;
            existing.TramitadorCuentasPagarOpcionId = entity.TramitadorCuentasPagarOpcionId;
            existing.TramitadorLiquidacionesOpcionId = entity.TramitadorLiquidacionesOpcionId;
            existing.ObservacionesLiquidaciones = entity.ObservacionesLiquidaciones;
            existing.FaltaPoliza = entity.FaltaPoliza;
            existing.FechaModificacion = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}