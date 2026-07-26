#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;
using SAF.Application.Pagos.Dtos;

namespace SAF.Services.Implementations;

public class PagosService(
    IDevengadoRepository devengadoRepo,
    IDevengadoExtraRepository extraRepo,
    ISadeRepository sadeRepo,
    ISigafOpRepository sigafRepo,
    IStatusContabilidadExtraRepository statusContabRepo,
    ICafRepository cafRepo,
    ISeguroRepository seguroRepo) : IPagosService
{
    private const string StatusAvanzarCaf = "avanzar CAF";

    public async Task<IReadOnlyList<PagoViewModel>> GetAllAsync(CancellationToken ct = default)
        => await MapearAsync(await devengadoRepo.GetAllAsync(ct), ct);

    public async Task<IReadOnlyList<PagoViewModel>> GetPageAsync(int skip, int take, CancellationToken ct = default)
        => await MapearAsync(await devengadoRepo.GetPageAsync(skip, take, ct), ct);

    /// <summary>
    /// FASE PROPIA: deriva todas las columnas que salen de las tablas de SAF. Las que
    /// dependen de IVC (Fecha/Buzón SADE, Fecha Pago No CAF y Fecha Pago Total) quedan
    /// vacías y las rellena CompletarIvcAsync con la grilla ya visible: la primera
    /// conexión a IVC puede tardar segundos y no debe frenar la primera pintada.
    /// Extras y tablero se traen completos porque solo tienen filas con datos manuales.
    /// </summary>
    private async Task<IReadOnlyList<PagoViewModel>> MapearAsync(
        IReadOnlyList<Devengado> devengados, CancellationToken ct)
    {
        // Un registro editable por FILA del ledger (DevengadoId), no por (TipoDev, NroDev).
        var extras = await extraRepo.GetAllAsync(ct);
        var extrasDict = extras.ToDictionary(e => e.DevengadoId);

        // STATUS CONTABLE / PEDIDO FACTURA 2·3 / FECHA FACTURA CORRECTA: cruce interno con
        // el tablero StatusContabilidadExtra por (TipoDev, NroDev).
        var statusContabs = await statusContabRepo.GetAllAsync(ct);
        var statusContabDict = statusContabs.ToDictionary(e => (e.TipoDev, e.NroDev));

        var expedientes = devengados
            .Where(d => !string.IsNullOrWhiteSpace(d.Expediente))
            .Select(d => d.Expediente!)
            .ToList();

        // FECHA PAGO CAF + SEGUROS TESO: tablas propias, en paralelo (cada repositorio
        // crea su propio DbContext vía IDbContextFactory).
        var cafTask = cafRepo.GetResumenCafByExpedientesAsync(expedientes, ct);
        var segurosTask = seguroRepo.GetSeguroByExpedientesAsync(expedientes, ct);
        await Task.WhenAll(cafTask, segurosTask);
        var cafPagos = cafTask.Result;
        var segurosTeso = segurosTask.Result;

        var result = new List<PagoViewModel>(devengados.Count);
        foreach (var d in devengados)
        {
            extrasDict.TryGetValue(d.Id, out var extra);
            statusContabDict.TryGetValue((d.TipoDev, d.NroDev), out var sc);
            var statusDgayf = extra?.StatusDgayfOpcion?.Nombre;
            // La fecha representa "el expediente está pagado", así que solo se informa
            // cuando TODAS sus OPs están pagadas. Con el MAX de las pagadas, 123 de los
            // 238 expedientes con varias OPs de la planilla real (52%) figuraban pagados
            // teniendo alguna OP pendiente; y el MAX nunca desempataba nada, porque las
            // OPs saldadas de un expediente comparten fecha.
            Application.Caf.Dtos.ResumenCafViewModel? resumenCaf =
                EsAvanzarCaf(statusDgayf) && d.Expediente is not null
                && cafPagos.TryGetValue(d.Expediente, out var rc) ? rc : null;
            DateTime? fechaDePagoCaf = resumenCaf?.TodasPagadas == true ? resumenCaf.UltimoPago : null;
            result.Add(new PagoViewModel
            {
                Id = d.Id,
                TipoDev = d.TipoDev,
                NroDev = d.NroDev,
                FechaDevengado = d.FechaImputacion,
                Expediente = d.Expediente,
                Empresa = d.Empresa,
                Importe = d.ImportePp,
                StatusDgayfOpcionId = extra?.StatusDgayfOpcionId,
                StatusDgayfNombre = statusDgayf,
                StatusOpOpcionId = extra?.StatusOpOpcionId,
                StatusOpNombre = extra?.StatusOpOpcion?.Nombre,
                FechaFirmaOp = extra?.FechaFirmaOp,
                Observaciones = extra?.Observaciones,
                Ccoo = extra?.Ccoo,
                FechaCcoo = extra?.FechaCcoo,
                FechaNotificacion = extra?.FechaNotificacion,
                StatusContable = DerivarStatusContable(extra?.StatusOpOpcion?.Nombre, sc?.StatusContableOpcion?.Nombre),
                SegurosTeso = d.Expediente is not null && segurosTeso.TryGetValue(d.Expediente, out var seg) ? seg : null,
                FechaDePagoCaf = fechaDePagoCaf,
                CafOps = resumenCaf?.Ops ?? 0,
                CafOpsPagadas = resumenCaf?.OpsPagadas ?? 0,
                CafLineas = resumenCaf?.Lineas ?? Array.Empty<Application.Caf.Dtos.LineaCafViewModel>(),
                // FechaDePagoNoCaf, FechaPagoTotal, FechaSade y BuzonSade las rellena
                // CompletarIvcAsync cuando responde IVC (la grilla ya está pintada).
                PedidoFactura2 = sc?.FechaPedidoFactura2,
                PedidoFactura3 = sc?.ReiterarPedidoFactura3,
                FechaFacturaCorrecta = sc?.FechaIngresoFactura,
                CafSiNo = extra?.CafSiNo,
                RowVersion = extra?.RowVersion,
            });
        }
        return result;
    }

    public async Task CompletarIvcAsync(IReadOnlyList<PagoViewModel> items, CancellationToken ct = default)
    {
        var expedientes = items
            .Where(p => !string.IsNullOrWhiteSpace(p.Expediente))
            .Select(p => p.Expediente!)
            .ToList();

        IReadOnlyDictionary<string, PaseSade> sade =
            new Dictionary<string, PaseSade>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyDictionary<string, DateTime> sigafPagos =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        if (expedientes.Count > 0)
        {
            // En paralelo: cada repositorio crea su propio DbContext (IDbContextFactory).
            var sadeTask = sadeRepo.GetByExpedientesAsync(expedientes, ct);
            var sigafTask = sigafRepo.GetFechaPagoByExpedientesAsync(expedientes, ct);
            await Task.WhenAll(sadeTask, sigafTask);
            sade = sadeTask.Result;
            sigafPagos = sigafTask.Result;
        }

        foreach (var p in items)
        {
            PaseSade? pase = p.Expediente is not null && sade.TryGetValue(p.Expediente, out var ps) ? ps : null;
            p.FechaSade = pase?.FechaUltimoPase;
            p.BuzonSade = pase?.BuzonDestino;
            p.FechaDePagoNoCaf = p.Expediente is not null && sigafPagos.TryGetValue(p.Expediente, out var fp)
                ? fp : null;
            // Misma fórmula que siempre; FechaDePagoCaf ya viene de la fase propia.
            p.FechaPagoTotal = CalcularFechaPagoTotal(p.StatusDgayfNombre, p.FechaDePagoNoCaf, p.FechaDePagoCaf);
        }
    }

    public async Task UpsertAsync(PagoViewModel vm, CancellationToken ct = default)
    {
        var errores = Application.Pagos.PagoValidator.ValidarEdicion(vm);
        if (errores.Count > 0) throw new ArgumentException(string.Join(" ", errores));

        var entity = new SAF.Data.Entities.DevengadoExtra
        {
            DevengadoId = vm.Id,
            TipoDev = vm.TipoDev,
            NroDev = vm.NroDev,
            StatusDgayfOpcionId = vm.StatusDgayfOpcionId,
            StatusOpOpcionId = vm.StatusOpOpcionId,
            FechaFirmaOp = vm.FechaFirmaOp,
            Observaciones = vm.Observaciones,
            Ccoo = vm.Ccoo,
            FechaCcoo = vm.FechaCcoo,
            FechaNotificacion = vm.FechaNotificacion,
            // Derivadas (no se persisten): StatusContable, SegurosTeso, PedidoFactura2/3,
            // FechaFacturaCorrecta, FechaDePagoNoCaf/Caf, FechaPagoTotal, FechaSade y BuzonSade.
            CafSiNo = vm.CafSiNo,
            // Versión que tenía el registro al cargarse: el upsert la exige para detectar
            // que otro usuario lo modificó mientras esta fila estaba en edición.
            RowVersion = vm.RowVersion,
        };
        await extraRepo.UpsertAsync(entity, ct);
    }

    public Task<DateTime?> GetUltimaFechaImputacionAsync(CancellationToken ct = default)
        => devengadoRepo.GetMaxFechaImputacionAsync(ct);

    /// <summary>
    /// ¿Ya hay una fila con el mismo tipo, número, fecha e importe? No es un error: un
    /// devengado puede tener líneas repetidas (existe un caso así en el histórico de IVC),
    /// pero casi siempre es una carga duplicada, así que la vista lo confirma con el usuario.
    /// </summary>
    public Task<bool> ExisteDevengadoIdenticoAsync(PagoViewModel vm, CancellationToken ct = default)
        => devengadoRepo.ExistsExactoAsync(
            (vm.TipoDev ?? string.Empty).Trim().ToUpperInvariant(), vm.NroDev, vm.FechaDevengado, vm.Importe, ct);

    public async Task<PagoViewModel> CreateDevengadoAsync(PagoViewModel vm, CancellationToken ct = default)
    {
        var errores = Application.Pagos.PagoValidator.ValidarAlta(vm);
        if (errores.Count > 0) throw new ArgumentException(string.Join(" ", errores));

        var tipoDev = (vm.TipoDev ?? string.Empty).Trim().ToUpperInvariant();
        var expediente = Application.Common.ExpedienteKey.Normalizar(vm.Expediente)!;

        var entity = new Devengado
        {
            TipoDev = tipoDev,
            NroDev = vm.NroDev,
            FechaImputacion = vm.FechaDevengado,
            Expediente = expediente,
            Empresa = string.IsNullOrWhiteSpace(vm.Empresa) ? null : vm.Empresa.Trim(),
            ImportePp = vm.Importe,
            FechaImportacion = DateTime.UtcNow,
        };
        await devengadoRepo.AddAsync(entity, ct);

        vm.Id = entity.Id;
        vm.TipoDev = tipoDev;
        vm.Expediente = expediente;
        return vm;
    }

    public Task DeleteDevengadoAsync(int devengadoId, byte[]? extraRowVersion, CancellationToken ct = default)
        => devengadoRepo.DeleteAsync(devengadoId, extraRowVersion, ct);

    private static bool EsAvanzarCaf(string? statusDgayf)
        => string.Equals(statusDgayf, StatusAvanzarCaf, StringComparison.OrdinalIgnoreCase);

    // FECHA PAGO TOTAL — Excel: =IF(StatusDGAyF="avanzar", P, IF(Q="","", MAX(P,Q)))
    //   P = Fecha Pago No CAF (derivada de SIGAF_OP)   Q = Fecha Pago CAF (derivada de tabla CAF)
    private static DateTime? CalcularFechaPagoTotal(string? statusDgayf, DateTime? noCaf, DateTime? caf)
    {
        if (string.Equals(statusDgayf, "avanzar", StringComparison.OrdinalIgnoreCase))
            return noCaf;
        if (caf is null)
            return null;
        return noCaf is DateTime n && n > caf.Value ? n : caf;
    }

    // STATUS CONTABLE — Excel: =IF(OR(StatusOP="OP Firmada",StatusOP="Pasado al pago BONO"),
    //   "OP Lista", <Status Contable del tablero>). Cruce interno por (TipoDev, NroDev).
    private static string? DerivarStatusContable(string? statusOp, string? statusContableTablero)
    {
        if (string.Equals(statusOp, "OP Firmada", StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusOp, "Pasado al pago BONO", StringComparison.OrdinalIgnoreCase))
            return "OP Lista";
        return statusContableTablero;
    }
}
