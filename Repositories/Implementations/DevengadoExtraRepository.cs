#nullable enable
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SAF.Application.ControlOp.Dtos;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class DevengadoExtraRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ExtraRepositoryBase<DevengadoExtra>(dbFactory), IDevengadoExtraRepository
{
    // Única definición de la consulta de lectura (navegaciones incluidas): la
    // comparten la carga completa y la búsqueda por clave, para que un Include
    // agregado a una no pueda faltar en la otra.
    private static IQueryable<DevengadoExtra> Query(AppDbContext db) =>
        db.DevengadosExtra.AsNoTracking()
            .Include(e => e.StatusDgayfOpcion)
            .Include(e => e.StatusOpOpcion);

    public async Task<IReadOnlyList<DevengadoExtra>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(ct);
        return await Query(db)
            .OrderBy(e => e.TipoDev)
            .ThenBy(e => e.NroDev)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DevengadoExtra>> GetByDevengadoIdsAsync(
        IReadOnlyCollection<int> devengadoIds, CancellationToken ct = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(ct);
        return await Query(db)
            .Where(e => devengadoIds.Contains(e.DevengadoId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ControlOpViewModel>> GetConFirmaOpAnteriorAsync(
        DateTime fechaCorte, IReadOnlyCollection<string> statusExcluidos, CancellationToken ct = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(ct);
        // Proyección y no entidades: el conjunto incluye todo devengado con OP firmada
        // (pagado o no, eso lo decide IVC después), así que crece con el ledger y no
        // conviene arrastrar las columnas anchas ni el lookup de Status OP.
        return await db.DevengadosExtra.AsNoTracking()
            .Where(e => e.FechaFirmaOp != null && e.FechaFirmaOp < fechaCorte
                     && (e.StatusDgayfOpcion == null || !statusExcluidos.Contains(e.StatusDgayfOpcion.Nombre)))
            // Las firmas más viejas primero: son las que más tiempo llevan sin cruzar.
            .OrderBy(e => e.FechaFirmaOp)
            .ThenBy(e => e.TipoDev)
            .ThenBy(e => e.NroDev)
            .ThenBy(e => e.Id)
            .Select(e => new ControlOpViewModel
            {
                DevengadoId = e.DevengadoId,
                TipoDev = e.Devengado!.TipoDev,
                NroDev = e.Devengado.NroDev,
                Expediente = e.Devengado.Expediente,
                Empresa = e.Devengado.Empresa,
                Importe = e.Devengado.ImportePp,
                FechaFirmaOp = e.FechaFirmaOp,
                StatusDgayf = e.StatusDgayfOpcion == null ? null : e.StatusDgayfOpcion.Nombre,
            })
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
