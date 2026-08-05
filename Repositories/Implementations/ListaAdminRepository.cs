#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class ListaAdminRepository(IDbContextFactory<AppDbContext> dbFactory) : IListaAdminRepository
{
    public async Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken ct = default) where T : class, IOpcionLista
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // EF.Property en lugar de x.Orden: el acceso vía miembro de interfaz no
        // siempre se traduce a SQL; por nombre de propiedad es traducción directa.
        return await db.Set<T>().AsNoTracking()
            .OrderBy(x => EF.Property<int>(x, nameof(IOpcionLista.Orden)))
            .ThenBy(x => EF.Property<int>(x, nameof(IOpcionLista.Id)))
            .ToListAsync(ct);
    }

    public async Task<T?> GetByIdAsync<T>(int id, CancellationToken ct = default) where T : class, IOpcionLista
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Set<T>().AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Property<int>(x, nameof(IOpcionLista.Id)) == id, ct);
    }

    public async Task AddAsync<T>(T entity, CancellationToken ct = default) where T : class, IOpcionLista
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Set<T>().Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync<T>(T entity, CancellationToken ct = default) where T : class, IOpcionLista
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Set<T>().Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync<T>(int id, CancellationToken ct = default) where T : class, IOpcionLista
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Set<T>()
            .FirstOrDefaultAsync(x => EF.Property<int>(x, nameof(IOpcionLista.Id)) == id, ct);
        if (entity is null) return; // ya no existe: el resultado buscado

        db.Set<T>().Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExisteNombreAsync<T>(string nombre, int idExcluido, CancellationToken ct = default) where T : class, IOpcionLista
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // La comparación corre en SQL Server con la collation de la base
        // (case-insensitive), que es el criterio real de unicidad que importa.
        return await db.Set<T>().AsNoTracking()
            .AnyAsync(x => EF.Property<string>(x, nameof(IOpcionLista.Nombre)) == nombre
                        && EF.Property<int>(x, nameof(IOpcionLista.Id)) != idExcluido, ct);
    }
}
