#nullable enable
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;
using SAF.Application.Common;
using SAF.Application.Seguros;
using SAF.Application.Seguros.Dtos;

namespace SAF.Services.Implementations;

public class SeguroService(ISeguroRepository repo, ILogger<SeguroService> logger) : ISeguroService
{
    public async Task<IReadOnlyList<SeguroViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await repo.GetAllAsync(ct);
        return rows.Select(ToVm).ToList();
    }

    public async Task<SeguroViewModel> CreateAsync(SeguroViewModel vm, CancellationToken ct = default)
    {
        Validar(vm);
        var entity = MapToEntity(vm, new ExpedienteSeguro());
        entity.FechaCreacion = DateTime.UtcNow;
        entity.FechaModificacion = DateTime.UtcNow;
        await repo.AddAsync(entity, ct);
        return ToVm(entity);
    }

    public async Task UpdateAsync(SeguroViewModel vm, CancellationToken ct = default)
    {
        Validar(vm);
        var entity = await repo.GetByIdAsync(vm.Id, ct);
        if (entity is null)
        {
            // Otro usuario la borró mientras esta se editaba: avisar como conflicto;
            // un retorno silencioso deja creer que se guardó.
            logger.LogWarning("Seguro {Id} ya no existe; no se guardaron los cambios.", vm.Id);
            throw new ConflictoDeConcurrenciaException(
                "Otro usuario eliminó esta fila mientras la editabas. Se recargaron los datos.");
        }

        MapToEntity(vm, entity);
        entity.FechaModificacion = DateTime.UtcNow;
        await repo.UpdateAsync(entity, ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => repo.DeleteAsync(id, ct);

    private static void Validar(SeguroViewModel vm)
    {
        var errores = SeguroValidator.Validar(vm);
        if (errores.Count > 0) throw new ArgumentException(string.Join(" ", errores));
    }

    private static ExpedienteSeguro MapToEntity(SeguroViewModel vm, ExpedienteSeguro e)
    {
        // Columna única: se guarda ya normalizado; formato inválido se rechaza.
        e.Expediente = ExpedienteKey.Normalizar(vm.Expediente)
            ?? throw new ArgumentException(
                $"Expediente inválido: \"{vm.Expediente}\". Formatos aceptados: {ExpedienteKey.FormatosAceptados}.");
        e.Op = vm.Op;
        e.Beneficiario = vm.Beneficiario;
        e.ImporteNeto = vm.ImporteNeto;
        e.Estado = vm.Estado;
        e.SeguroOpcionId = vm.SeguroOpcionId;
        // La versión que tenía la fila cuando el usuario la cargó: el UPDATE la exige
        // en el WHERE, así que si otro la guardó mientras tanto, no afecta ninguna fila.
        e.RowVersion = vm.RowVersion;
        return e;
    }

    private static SeguroViewModel ToVm(ExpedienteSeguro e) => new()
    {
        Id = e.Id,
        Expediente = e.Expediente,
        Op = e.Op,
        Beneficiario = e.Beneficiario,
        ImporteNeto = e.ImporteNeto,
        Estado = e.Estado,
        SeguroOpcionId = e.SeguroOpcionId,
        SeguroNombre = e.SeguroOpcion?.Nombre,
        RowVersion = e.RowVersion,
    };
}
