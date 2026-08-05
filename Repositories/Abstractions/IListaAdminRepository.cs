#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

/// <summary>
/// CRUD genérico de las tablas de opciones (IOpcionLista). A diferencia de
/// ILookupRepository (que alimenta los desplegables con las opciones activas),
/// acá se devuelven TODAS las filas: la administración también ve las inactivas.
/// </summary>
public interface IListaAdminRepository
{
    Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken ct = default) where T : class, IOpcionLista;
    Task<T?> GetByIdAsync<T>(int id, CancellationToken ct = default) where T : class, IOpcionLista;
    Task AddAsync<T>(T entity, CancellationToken ct = default) where T : class, IOpcionLista;
    Task UpdateAsync<T>(T entity, CancellationToken ct = default) where T : class, IOpcionLista;
    Task DeleteAsync<T>(int id, CancellationToken ct = default) where T : class, IOpcionLista;

    /// <summary>True si ya existe otra fila (distinta de idExcluido) con ese nombre.</summary>
    Task<bool> ExisteNombreAsync<T>(string nombre, int idExcluido, CancellationToken ct = default) where T : class, IOpcionLista;
}
