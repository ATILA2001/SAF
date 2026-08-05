#nullable enable
using SAF.Application.AdminListas.Dtos;

namespace SAF.Services.Abstractions;

/// <summary>
/// Administración (alta/edición/baja) de las listas de opciones. La lista se
/// identifica por su clave del catálogo ListasAdmin (ej: "status-dgayf").
/// </summary>
public interface IListaAdminService
{
    Task<IReadOnlyList<OpcionListaViewModel>> GetAllAsync(string listaKey, CancellationToken ct = default);
    Task<OpcionListaViewModel> CreateAsync(string listaKey, OpcionListaViewModel vm, CancellationToken ct = default);
    Task UpdateAsync(string listaKey, OpcionListaViewModel vm, CancellationToken ct = default);
    Task DeleteAsync(string listaKey, int id, CancellationToken ct = default);
}
