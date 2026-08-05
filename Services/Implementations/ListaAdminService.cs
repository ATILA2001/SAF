#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.AdminListas;
using SAF.Application.AdminListas.Dtos;
using SAF.Application.Common;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;

namespace SAF.Services.Implementations;

/// <summary>
/// CRUD de las listas de opciones. La clave del catálogo se traduce acá a la
/// entidad concreta; el resto es genérico porque todas comparten IOpcionLista
/// (Seguros agrega EsOk y se contempla como caso especial en el mapeo).
/// </summary>
public class ListaAdminService(IListaAdminRepository repo, ILogger<ListaAdminService> logger) : IListaAdminService
{
    public Task<IReadOnlyList<OpcionListaViewModel>> GetAllAsync(string listaKey, CancellationToken ct = default)
        => listaKey switch
        {
            ListasAdmin.StatusDgayf => GetAllAsync<StatusDgayfOpcion>(ct),
            ListasAdmin.StatusOp => GetAllAsync<StatusOpOpcion>(ct),
            ListasAdmin.StatusContable => GetAllAsync<StatusContableOpcion>(ct),
            ListasAdmin.TramitadoresCuentasPagar => GetAllAsync<TramitadorCuentasPagarOpcion>(ct),
            ListasAdmin.TramitadoresLiquidaciones => GetAllAsync<TramitadorLiquidacionesOpcion>(ct),
            ListasAdmin.Seguros => GetAllAsync<SeguroOpcion>(ct),
            _ => throw new ArgumentException($"Lista desconocida: \"{listaKey}\"."),
        };

    public Task<OpcionListaViewModel> CreateAsync(string listaKey, OpcionListaViewModel vm, CancellationToken ct = default)
        => listaKey switch
        {
            ListasAdmin.StatusDgayf => CreateAsync<StatusDgayfOpcion>(vm, ct),
            ListasAdmin.StatusOp => CreateAsync<StatusOpOpcion>(vm, ct),
            ListasAdmin.StatusContable => CreateAsync<StatusContableOpcion>(vm, ct),
            ListasAdmin.TramitadoresCuentasPagar => CreateAsync<TramitadorCuentasPagarOpcion>(vm, ct),
            ListasAdmin.TramitadoresLiquidaciones => CreateAsync<TramitadorLiquidacionesOpcion>(vm, ct),
            ListasAdmin.Seguros => CreateAsync<SeguroOpcion>(vm, ct),
            _ => throw new ArgumentException($"Lista desconocida: \"{listaKey}\"."),
        };

    public Task UpdateAsync(string listaKey, OpcionListaViewModel vm, CancellationToken ct = default)
        => listaKey switch
        {
            ListasAdmin.StatusDgayf => UpdateAsync<StatusDgayfOpcion>(vm, ct),
            ListasAdmin.StatusOp => UpdateAsync<StatusOpOpcion>(vm, ct),
            ListasAdmin.StatusContable => UpdateAsync<StatusContableOpcion>(vm, ct),
            ListasAdmin.TramitadoresCuentasPagar => UpdateAsync<TramitadorCuentasPagarOpcion>(vm, ct),
            ListasAdmin.TramitadoresLiquidaciones => UpdateAsync<TramitadorLiquidacionesOpcion>(vm, ct),
            ListasAdmin.Seguros => UpdateAsync<SeguroOpcion>(vm, ct),
            _ => throw new ArgumentException($"Lista desconocida: \"{listaKey}\"."),
        };

    public Task DeleteAsync(string listaKey, int id, CancellationToken ct = default)
        => listaKey switch
        {
            ListasAdmin.StatusDgayf => DeleteAsync<StatusDgayfOpcion>(id, ct),
            ListasAdmin.StatusOp => DeleteAsync<StatusOpOpcion>(id, ct),
            ListasAdmin.StatusContable => DeleteAsync<StatusContableOpcion>(id, ct),
            ListasAdmin.TramitadoresCuentasPagar => DeleteAsync<TramitadorCuentasPagarOpcion>(id, ct),
            ListasAdmin.TramitadoresLiquidaciones => DeleteAsync<TramitadorLiquidacionesOpcion>(id, ct),
            ListasAdmin.Seguros => DeleteAsync<SeguroOpcion>(id, ct),
            _ => throw new ArgumentException($"Lista desconocida: \"{listaKey}\"."),
        };

    private async Task<IReadOnlyList<OpcionListaViewModel>> GetAllAsync<T>(CancellationToken ct) where T : class, IOpcionLista
    {
        var rows = await repo.GetAllAsync<T>(ct);
        return rows.Select(ToVm).ToList();
    }

    private async Task<OpcionListaViewModel> CreateAsync<T>(OpcionListaViewModel vm, CancellationToken ct)
        where T : class, IOpcionLista, new()
    {
        Validar(vm);
        await ValidarNombreUnicoAsync<T>(vm, ct);

        var entity = MapToEntity(vm, new T());
        await repo.AddAsync(entity, ct);
        return ToVm(entity);
    }

    private async Task UpdateAsync<T>(OpcionListaViewModel vm, CancellationToken ct) where T : class, IOpcionLista
    {
        Validar(vm);
        await ValidarNombreUnicoAsync<T>(vm, ct);

        var entity = await repo.GetByIdAsync<T>(vm.Id, ct);
        if (entity is null)
        {
            // Otro usuario la borró mientras esta se editaba: avisar como conflicto;
            // un retorno silencioso deja creer que se guardó.
            logger.LogWarning("Opción {Id} de {Lista} ya no existe; no se guardaron los cambios.", vm.Id, typeof(T).Name);
            throw new ConflictoDeConcurrenciaException(
                "Otro usuario eliminó esta opción mientras la editabas. Se recargaron los datos.");
        }

        MapToEntity(vm, entity);
        await repo.UpdateAsync(entity, ct);
    }

    private async Task DeleteAsync<T>(int id, CancellationToken ct) where T : class, IOpcionLista
    {
        try
        {
            await repo.DeleteAsync<T>(id, ct);
        }
        catch (DbUpdateException ex)
        {
            // Las FKs de las vistas apuntan a estas tablas sin cascada: si la opción
            // está referenciada, el DELETE falla. El camino correcto es desactivarla.
            logger.LogWarning(ex, "No se pudo eliminar la opción {Id} de {Lista} (en uso).", id, typeof(T).Name);
            throw new ArgumentException(
                "No se puede eliminar: la opción está en uso en registros existentes. Desactivala en su lugar.");
        }
    }

    private static void Validar(OpcionListaViewModel vm)
    {
        var errores = OpcionListaValidator.Validar(vm);
        if (errores.Count > 0) throw new ArgumentException(string.Join(" ", errores));
    }

    private async Task ValidarNombreUnicoAsync<T>(OpcionListaViewModel vm, CancellationToken ct) where T : class, IOpcionLista
    {
        if (await repo.ExisteNombreAsync<T>(vm.Nombre.Trim(), vm.Id, ct))
            throw new ArgumentException($"Ya existe una opción \"{vm.Nombre.Trim()}\" en esta lista.");
    }

    private static T MapToEntity<T>(OpcionListaViewModel vm, T e) where T : class, IOpcionLista
    {
        e.Nombre = vm.Nombre.Trim();
        e.Orden = vm.Orden;
        e.Activo = vm.Activo;
        if (e is SeguroOpcion seguro) seguro.EsOk = vm.EsOk == true;
        return e;
    }

    private static OpcionListaViewModel ToVm<T>(T e) where T : class, IOpcionLista => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Orden = e.Orden,
        Activo = e.Activo,
        EsOk = e is SeguroOpcion seguro ? seguro.EsOk : null,
    };
}
