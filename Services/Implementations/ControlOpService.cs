#nullable enable
using SAF.Application.Common;
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;
using SAF.Application.Pagos;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;

namespace SAF.Services.Implementations;

public class ControlOpService(
    IDevengadoExtraRepository extraRepo,
    IDevengadoRepository devengadoRepo,
    ISigafOpRepository sigafRepo,
    ISigafOpCorreccionRepository correccionRepo,
    ILogger<ControlOpService> logger) : IControlOpService
{
    // Igualdad de importes al peso: las diferencias de centavos entre el devengado y la
    // OP existen (redondeos de la bajada) y no deben leerse como "pago parcial".
    private const decimal ToleranciaImporte = 1m;

    public async Task<IReadOnlyList<ControlOpViewModel>> GetListaControlAsync(
        DateTime fechaCorte, CancellationToken ct = default)
    {
        // Firma, status y fecha de corte se filtran en SQL; la condición que falta ("sin
        // fecha de pago") sale de IVC, así que se trae lo demás y se termina acá.
        var filas = await extraRepo.GetConFirmaOpAnteriorAsync(fechaCorte, ReglasControlOp.StatusExcluidos, ct);

        var expedientes = filas
            .Select(f => f.Expediente)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var fechasNoCaf = await sigafRepo.GetFechaPagoByExpedientesAsync(expedientes, ct);

        return filas.Where(f =>
        {
            DateTime? noCaf = f.Expediente is not null && fechasNoCaf.TryGetValue(f.Expediente, out var fecha) ? fecha : null;
            // Misma fórmula que la columna de Pagos. La fecha CAF va en null a propósito:
            // en Pagos solo se informa con status "avanzar CAF", que acá está excluido.
            var fechaPagoTotal = ReglasPago.CalcularFechaPagoTotal(f.StatusDgayf, noCaf, caf: null);
            return ReglasControlOp.EstaPendiente(f.FechaFirmaOp, f.StatusDgayf, fechaPagoTotal, fechaCorte);
        }).ToList();
    }

    public async Task<IReadOnlyList<CandidataOpViewModel>> BuscarCandidatasAsync(
        CriteriosBusquedaOp criterios, CancellationToken ct = default)
    {
        if (!criterios.Acota)
            throw new ArgumentException("Indicá al menos el beneficiario o un rango de fecha de pago para buscar.");

        var ops = await sigafRepo.BuscarOpsAsync(criterios, ct);
        if (ops.Count == 0) return Array.Empty<CandidataOpViewModel>();

        var candidatas = ops.Select(o => new CandidataOpViewModel
        {
            Op = o.Op,
            NroDocResp = o.NroDocResp,
            ExpedienteSigaf = ExpedienteBajada.Normalizar(o.EeFinanciera),
            Beneficiario = o.Beneficiario,
            ImporteOp = o.ImporteOp,
            FechaPago = o.FechaPago,
            Coincidencia = Comparar(o.ImporteOp, criterios.ImporteReferencia),
            CorregidaEnIvc = EsCorregidaEnIvc(o),
        }).ToList();

        // Señales de SAF: si el expediente de la OP existe en el ledger y si la OP ya
        // tiene corrección. En paralelo: cada repositorio crea su propio DbContext.
        var expedientes = candidatas.Select(c => c.ExpedienteSigaf).Where(x => x is not null).Select(x => x!).ToList();
        var existentesTask = devengadoRepo.ExistenExpedientesAsync(expedientes, ct);
        var correccionesTask = correccionRepo.GetClavesAsync(ct);
        await Task.WhenAll(existentesTask, correccionesTask);

        var corregidas = correccionesTask.Result.ToDictionary(
            c => FechaPagoSigaf.Clave(c.NroDocResp, c.Op), c => c.ExpedienteCorregido, StringComparer.OrdinalIgnoreCase);

        foreach (var c in candidatas)
        {
            // Sin expediente en la bajada también cuenta como "no está en devengados".
            c.ExpedienteSinDevengado = c.ExpedienteSigaf is null || !existentesTask.Result.Contains(c.ExpedienteSigaf);
            c.CorregidaEnSafA = c.NroDocResp is long nro && corregidas.TryGetValue(FechaPagoSigaf.Clave(nro, c.Op), out var a) ? a : null;
        }
        return candidatas;
    }

    // Regla de la bajada (ver SigafOpPago): corregida en IVC = CORRECION distinto de 0 y
    // EXP_CORREGIDO cargado, con el mismo filtro de "False" que EE_FINANCIERA.
    private static bool EsCorregidaEnIvc(OpSigafResumen o)
    {
        var marca = o.CorrecionIvc?.Trim();
        if (string.IsNullOrEmpty(marca) || marca == "0") return false;
        return ExpedienteBajada.Normalizar(o.ExpCorregidoIvc) is not null;
    }

    private static CoincidenciaImporte Comparar(decimal? importeOp, decimal? referencia)
    {
        if (referencia is not decimal r || importeOp is not decimal i) return CoincidenciaImporte.SinReferencia;
        if (Math.Abs(i - r) < ToleranciaImporte) return CoincidenciaImporte.Igual;
        return i < r ? CoincidenciaImporte.Parcial : CoincidenciaImporte.Mayor;
    }

    public async Task<IReadOnlyList<CorreccionOpViewModel>> GetCorreccionesAsync(CancellationToken ct = default)
        => (await correccionRepo.GetAllAsync(ct)).Select(ToVm).ToList();

    public async Task<CorreccionOpViewModel?> GetCorreccionByIdAsync(int id, CancellationToken ct = default)
        => await correccionRepo.GetByIdAsync(id, ct) is { } entity ? ToVm(entity) : null;

    public async Task<CorreccionOpViewModel> CreateAsync(CorreccionOpViewModel vm, CancellationToken ct = default)
    {
        Validar(vm);
        var op = vm.Op.Trim();
        var expediente = ExpedienteKey.Normalizar(vm.ExpedienteCorregido)!;

        // En paralelo (bases distintas); los errores se evalúan en el orden de siempre.
        var enBajadaTask = sigafRepo.GetOpAsync(vm.NroDocResp, op, ct);
        var existenteTask = correccionRepo.GetByClaveAsync(vm.NroDocResp, op, ct);
        await Task.WhenAll(enBajadaTask, existenteTask);

        // La OP tiene que existir en la bajada con ese NRO_DOC_RESP: es lo único que
        // protege de un error de tipeo, que dejaría una corrección que nunca cruza con nada.
        var enBajada = enBajadaTask.Result
            ?? throw new ArgumentException(
                $"La OP {op} con NRO DOC RESP {vm.NroDocResp} no existe en la bajada de OP. Revisá los dos datos.");

        if (existenteTask.Result is { } existente)
            throw new ArgumentException(
                $"La OP {op} ya tiene una corrección (expediente {existente.ExpedienteCorregido}). Editala en vez de crear otra.");

        // Por clave normalizada, igual que el cruce de Pagos (que pide las variantes crudas
        // y normaliza lo que vuelve) y que la guarda de la edición: una corrección al
        // mismo expediente que ya trae la bajada no cambia nada.
        if (string.Equals(ExpedienteBajada.Normalizar(enBajada.EeFinanciera), expediente, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"La OP {op} ya figura en la bajada de OP con el expediente {expediente}: no hace falta corregirla.");

        // Foto de la bajada acotada al largo de las columnas: un texto desbordado
        // volvería como error de truncamiento de SQL después de pasar todas las reglas.
        var entity = new SigafOpCorreccion
        {
            NroDocResp = vm.NroDocResp,
            Op = op,
            ExpedienteOriginal = Truncar(ExpedienteBajada.Normalizar(enBajada.EeFinanciera), 60),
            ExpedienteCorregido = expediente,
            Beneficiario = Truncar(enBajada.Beneficiario, 255),
            ImporteOp = enBajada.ImporteOp,
            FechaPago = enBajada.FechaPago,
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = DateTime.UtcNow,
        };
        await correccionRepo.AddAsync(entity, ct);

        logger.LogInformation("Corrección de OP: {Op} ({Nro}) de {Original} a {Corregido}.",
            op, vm.NroDocResp, entity.ExpedienteOriginal ?? "(vacío)", expediente);

        return ToVm(entity);
    }

    public async Task UpdateAsync(CorreccionOpViewModel vm, CancellationToken ct = default)
    {
        Validar(vm);
        var entity = await correccionRepo.GetByIdAsync(vm.Id, ct);
        if (entity is null)
        {
            // Otro usuario la borró mientras esta se editaba: avisar como conflicto;
            // un retorno silencioso deja creer que se guardó.
            logger.LogWarning("Corrección de OP {Id} ya no existe; no se guardaron los cambios.", vm.Id);
            throw new ConflictoDeConcurrenciaException(
                "Otro usuario eliminó esta fila mientras la editabas. Se recargaron los datos.");
        }

        // Solo cambia el expediente corregido: la OP es la identidad de la fila.
        var expediente = ExpedienteKey.Normalizar(vm.ExpedienteCorregido)!;

        // Misma guarda que el alta: volver al expediente que trae la bajada no es una
        // corrección (quedaría una fila que reasigna la OP a donde ya está); se elimina.
        if (string.Equals(entity.ExpedienteOriginal, expediente, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"La OP {entity.Op} ya figura en la bajada de OP con el expediente {expediente}: " +
                "si querés dejarla así, eliminá la corrección en vez de editarla.");

        entity.ExpedienteCorregido = expediente;
        entity.FechaModificacion = DateTime.UtcNow;
        // La versión que tenía la fila cuando el usuario la cargó: el UPDATE la exige
        // en el WHERE, así que si otro la guardó mientras tanto, no afecta ninguna fila.
        entity.RowVersion = vm.RowVersion;
        await correccionRepo.UpdateAsync(entity, ct);
    }

    public Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default)
        => correccionRepo.DeleteAsync(id, rowVersion, ct);

    private static string? Truncar(string? valor, int max) =>
        valor is null || valor.Length <= max ? valor : valor[..max];

    private static void Validar(CorreccionOpViewModel vm)
    {
        var errores = CorreccionOpValidator.Validar(vm);
        if (errores.Count > 0) throw new ArgumentException(string.Join(" ", errores));
    }

    private static CorreccionOpViewModel ToVm(SigafOpCorreccion e) => new()
    {
        Id = e.Id,
        NroDocResp = e.NroDocResp,
        Op = e.Op,
        ExpedienteOriginal = e.ExpedienteOriginal,
        ExpedienteCorregido = e.ExpedienteCorregido,
        Beneficiario = e.Beneficiario,
        ImporteOp = e.ImporteOp,
        FechaPago = e.FechaPago,
        // Se persiste en UTC (como el resto de las tablas); la grilla y el export la
        // muestran en hora local.
        FechaModificacion = DateTime.SpecifyKind(e.FechaModificacion, DateTimeKind.Utc).ToLocalTime(),
        RowVersion = e.RowVersion,
    };
}
