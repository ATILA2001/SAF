#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.Common;
using SAF.Application.ControlOp;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class SigafOpCorreccionRepository(IDbContextFactory<AppDbContext> dbFactory) : ISigafOpCorreccionRepository
{
    public async Task<IReadOnlyList<SigafOpCorreccion>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Las más recientes arriba: lo que se acaba de corregir es lo que se revisa.
        return await db.SigafOpCorrecciones.AsNoTracking()
            .OrderByDescending(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task<SigafOpCorreccion?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.SigafOpCorrecciones.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<SigafOpCorreccion?> GetByClaveAsync(long nroDocResp, string op, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // La columna es nvarchar con collation CI: la igualdad no distingue mayúsculas.
        return await db.SigafOpCorrecciones.AsNoTracking()
            .FirstOrDefaultAsync(e => e.NroDocResp == nroDocResp && e.Op == op, ct);
    }

    public async Task<IReadOnlyList<CorreccionOpClave>> GetClavesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.SigafOpCorrecciones.AsNoTracking()
            .Select(e => new CorreccionOpClave(e.NroDocResp, e.Op, e.ExpedienteCorregido))
            .ToListAsync(ct);
    }

    public async Task AddAsync(SigafOpCorreccion entity, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.SigafOpCorrecciones.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ErroresSql.EsClaveDuplicada(ex))
        {
            // Otro usuario corrigió la misma OP entre el chequeo del servicio y el
            // guardado: mismo aviso que si la corrección ya existía.
            throw new ArgumentException(
                $"La OP {entity.Op} ya tiene una corrección cargada por otro usuario. Editala en vez de crear otra.");
        }
    }

    public async Task UpdateAsync(SigafOpCorreccion entity, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.SigafOpCorrecciones.Update(entity);

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
        var e = await db.SigafOpCorrecciones.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return; // ya no existe: el resultado buscado

        // Mismo criterio fail-closed que CAF y Seguros: el DELETE exige la versión que
        // el usuario tenía en pantalla; sin versión no hay contra qué comparar.
        if (rowVersion is null)
            throw new ConflictoDeConcurrenciaException();
        db.Entry(e).Property(x => x.RowVersion).OriginalValue = rowVersion;

        db.SigafOpCorrecciones.Remove(e);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoDeConcurrenciaException();
        }
    }
}
