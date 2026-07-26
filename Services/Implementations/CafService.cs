#nullable enable
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;
using SAF.Application.Common;
using SAF.Application.Caf;
using SAF.Application.Caf.Dtos;

namespace SAF.Services.Implementations;

public class CafService(ICafRepository repo, ILogger<CafService> logger) : ICafService
{
    public async Task<IReadOnlyList<CafViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await repo.GetAllAsync(ct);
        return rows.Select(ToVm).ToList();
    }

    public async Task<CafViewModel> CreateAsync(CafViewModel vm, CancellationToken ct = default)
    {
        Validar(vm);
        var entity = MapToEntity(vm, new ExpedienteCaf());
        entity.FechaCreacion = DateTime.UtcNow;
        entity.FechaModificacion = DateTime.UtcNow;
        await repo.AddAsync(entity, ct);
        return ToVm(entity);
    }

    public async Task UpdateAsync(CafViewModel vm, CancellationToken ct = default)
    {
        Validar(vm);
        var entity = await repo.GetByIdAsync(vm.Id, ct);
        if (entity is null)
        {
            // Otro usuario la borró mientras esta se editaba: avisar como conflicto;
            // un retorno silencioso deja creer que se guardó.
            logger.LogWarning("Expediente CAF {Id} ya no existe; no se guardaron los cambios.", vm.Id);
            throw new ConflictoDeConcurrenciaException(
                "Otro usuario eliminó esta fila mientras la editabas. Se recargaron los datos.");
        }

        MapToEntity(vm, entity);
        entity.FechaModificacion = DateTime.UtcNow;
        await repo.UpdateAsync(entity, ct);
    }

    public Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default)
        => repo.DeleteAsync(id, rowVersion, ct);

    private static void Validar(CafViewModel vm)
    {
        var errores = CafValidator.Validar(vm);
        if (errores.Count > 0) throw new ArgumentException(string.Join(" ", errores));
    }

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
        // La versión que tenía la fila cuando el usuario la cargó: el UPDATE la exige
        // en el WHERE, así que si otro la guardó mientras tanto, no afecta ninguna fila.
        e.RowVersion = vm.RowVersion;
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
        RowVersion = e.RowVersion,
    };
}
