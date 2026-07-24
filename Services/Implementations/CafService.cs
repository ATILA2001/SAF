#nullable enable
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;
using SAF.Application.Common;
using SAF.Application.Caf.Dtos;

namespace SAF.Services.Implementations;

public class CafService(ICafRepository repo) : ICafService
{
    public async Task<IReadOnlyList<CafViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await repo.GetAllAsync(ct);
        return rows.Select(ToVm).ToList();
    }

    public async Task<CafViewModel> CreateAsync(CafViewModel vm, CancellationToken ct = default)
    {
        var entity = MapToEntity(vm, new ExpedienteCaf());
        entity.FechaCreacion = DateTime.UtcNow;
        entity.FechaModificacion = DateTime.UtcNow;
        await repo.AddAsync(entity, ct);
        return ToVm(entity);
    }

    public async Task UpdateAsync(CafViewModel vm, CancellationToken ct = default)
    {
        var entity = await repo.GetByIdAsync(vm.Id, ct);
        if (entity is null) return;

        MapToEntity(vm, entity);
        entity.FechaModificacion = DateTime.UtcNow;
        await repo.UpdateAsync(entity, ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => repo.DeleteAsync(id, ct);

    private static ExpedienteCaf MapToEntity(CafViewModel vm, ExpedienteCaf e)
    {
        // Columna única: se guarda ya normalizado; formato inválido se rechaza.
        e.Expediente = ExpedienteKey.Normalizar(vm.Expediente)
            ?? throw new ArgumentException(
                $"Expediente inválido: \"{vm.Expediente}\". Formatos aceptados: {ExpedienteKey.FormatosAceptados}.");
        e.Anio = vm.Anio;
        e.Op = vm.Op;
        e.Beneficiario = vm.Beneficiario;
        e.ImporteNeto = vm.ImporteNeto;
        e.Iibb = vm.Iibb;
        e.Cuenta = vm.Cuenta;
        e.FechaPago = vm.FechaPago;
        e.Cargado = vm.Cargado;
        e.CcPagadora = vm.CcPagadora;
        e.Pase = vm.Pase;
        e.Revisado = vm.Revisado;
        return e;
    }

    private static CafViewModel ToVm(ExpedienteCaf e) => new()
    {
        Id = e.Id,
        Anio = e.Anio,
        Expediente = e.Expediente,
        Op = e.Op,
        Beneficiario = e.Beneficiario,
        ImporteNeto = e.ImporteNeto,
        Iibb = e.Iibb,
        Cuenta = e.Cuenta,
        FechaPago = e.FechaPago,
        Cargado = e.Cargado,
        CcPagadora = e.CcPagadora,
        Pase = e.Pase,
        Revisado = e.Revisado,
    };
}
