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
    {
        var devengados = await devengadoRepo.GetAllAsync(ct);

        // Un registro editable por FILA del ledger (DevengadoId), no por (TipoDev, NroDev).
        var extras = await extraRepo.GetAllAsync(ct);
        var extrasDict = extras.ToDictionary(e => e.DevengadoId);

        // STATUS CONTABLE / PEDIDO FACTURA 2·3 / FECHA FACTURA CORRECTA: cruce interno con
        // el tablero StatusContabilidadExtra por (TipoDev, NroDev).
        var statusContabs = await statusContabRepo.GetAllAsync(ct);
        var statusContabDict = statusContabs.ToDictionary(e => (e.TipoDev, e.NroDev));

        // Lookups por expediente, filtrados: las tablas IVC cubren MUCHOS más expedientes
        // que la grilla (PASES_SADE ~53k vs ~1,6k del ledger), así que el filtro reduce
        // fuerte lo que viaja. IVC y SAF son DbContexts distintos → sus pipelines de
        // consultas corren en paralelo (cada uno secuencial sobre su propio contexto).
        var expedientes = devengados
            .Where(d => !string.IsNullOrWhiteSpace(d.Expediente))
            .Select(d => d.Expediente!)
            .ToList();

        var ivcTask = LoadIvcAsync();     // FECHA/BUZÓN SADE + FECHA PAGO NO CAF
        var propiasTask = LoadPropiasAsync(); // FECHA PAGO CAF + SEGUROS TESO
        await Task.WhenAll(ivcTask, propiasTask);
        var (sade, sigafPagos) = ivcTask.Result;
        var (cafPagos, segurosTeso) = propiasTask.Result;

        async Task<(IReadOnlyDictionary<string, PaseSade>, IReadOnlyDictionary<string, DateTime>)> LoadIvcAsync()
            => (await sadeRepo.GetByExpedientesAsync(expedientes, ct),
                await sigafRepo.GetFechaPagoByExpedientesAsync(expedientes, ct));

        async Task<(IReadOnlyDictionary<string, DateTime>, IReadOnlyDictionary<string, string>)> LoadPropiasAsync()
            => (await cafRepo.GetFechaPagoCafByExpedientesAsync(expedientes, ct),
                await seguroRepo.GetSeguroByExpedientesAsync(expedientes, ct));

        var result = new List<PagoViewModel>(devengados.Count);
        foreach (var d in devengados)
        {
            extrasDict.TryGetValue(d.Id, out var extra);
            statusContabDict.TryGetValue((d.TipoDev, d.NroDev), out var sc);
            var statusDgayf = extra?.StatusDgayfOpcion?.Nombre;
            PaseSade? pase = d.Expediente is not null && sade.TryGetValue(d.Expediente, out var ps) ? ps : null;
            DateTime? fechaPagoNoCaf = d.Expediente is not null && sigafPagos.TryGetValue(d.Expediente, out var fp) ? fp : null;
            DateTime? fechaDePagoCaf = EsAvanzarCaf(statusDgayf) && d.Expediente is not null
                && cafPagos.TryGetValue(d.Expediente, out var fc) ? fc : null;
            DateTime? fechaPagoTotal = CalcularFechaPagoTotal(statusDgayf, fechaPagoNoCaf, fechaDePagoCaf);
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
                FechaDePagoNoCaf = fechaPagoNoCaf,
                FechaDePagoCaf = fechaDePagoCaf,
                FechaPagoTotal = fechaPagoTotal,
                FechaSade = pase?.FechaUltimoPase,
                BuzonSade = pase?.BuzonDestino,
                PedidoFactura2 = sc?.FechaPedidoFactura2,
                PedidoFactura3 = sc?.ReiterarPedidoFactura3,
                FechaFacturaCorrecta = sc?.FechaIngresoFactura,
                CafSiNo = extra?.CafSiNo,
            });
        }
        return result;
    }

    public async Task<PagoViewModel?> GetByIdAsync(int devengadoId, CancellationToken ct = default)
    {
        var d = await devengadoRepo.GetByIdAsync(devengadoId, ct);
        if (d is null) return null;

        var extra = await extraRepo.GetByDevengadoIdAsync(devengadoId, ct);
        var sc = await statusContabRepo.GetByKeyAsync(d.TipoDev, d.NroDev, ct);
        var statusDgayf = extra?.StatusDgayfOpcion?.Nombre;
        PaseSade? pase = null;
        DateTime? fechaPagoNoCaf = null;
        DateTime? fechaDePagoCaf = null;
        string? segurosTeso = null;
        if (!string.IsNullOrWhiteSpace(d.Expediente))
        {
            var sade = await sadeRepo.GetByExpedientesAsync(new[] { d.Expediente }, ct);
            sade.TryGetValue(d.Expediente, out pase);

            var seguros = await seguroRepo.GetSeguroByExpedientesAsync(new[] { d.Expediente }, ct);
            seguros.TryGetValue(d.Expediente, out segurosTeso);

            var sigafPagos = await sigafRepo.GetFechaPagoByExpedientesAsync(new[] { d.Expediente }, ct);
            if (sigafPagos.TryGetValue(d.Expediente, out var fp)) fechaPagoNoCaf = fp;

            if (EsAvanzarCaf(statusDgayf))
            {
                var cafPagos = await cafRepo.GetFechaPagoCafByExpedientesAsync(new[] { d.Expediente }, ct);
                if (cafPagos.TryGetValue(d.Expediente, out var fc)) fechaDePagoCaf = fc;
            }
        }
        DateTime? fechaPagoTotal = CalcularFechaPagoTotal(statusDgayf, fechaPagoNoCaf, fechaDePagoCaf);
        return new PagoViewModel
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
            SegurosTeso = segurosTeso,
            FechaDePagoNoCaf = fechaPagoNoCaf,
            FechaDePagoCaf = fechaDePagoCaf,
            FechaPagoTotal = fechaPagoTotal,
            FechaSade = pase?.FechaUltimoPase,
            BuzonSade = pase?.BuzonDestino,
            PedidoFactura2 = sc?.FechaPedidoFactura2,
            PedidoFactura3 = sc?.ReiterarPedidoFactura3,
            FechaFacturaCorrecta = sc?.FechaIngresoFactura,
            CafSiNo = extra?.CafSiNo,
        };
    }

    public async Task UpsertAsync(PagoViewModel vm, CancellationToken ct = default)
    {
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
        };
        await extraRepo.UpsertAsync(entity, ct);
    }

    public Task<DateTime?> GetUltimaFechaImputacionAsync(CancellationToken ct = default)
        => devengadoRepo.GetMaxFechaImputacionAsync(ct);

    // Tipos que la vista Pagos excluye (mismo filtro que el sync con IVC).
    private static readonly string[] TiposExcluidos = ["C55", "CPS"];

    public async Task<PagoViewModel> CreateDevengadoAsync(PagoViewModel vm, CancellationToken ct = default)
    {
        var tipoDev = (vm.TipoDev ?? string.Empty).Trim().ToUpperInvariant();
        if (tipoDev.Length == 0)
            throw new ArgumentException("El tipo de devengado es obligatorio (ej.: PRD, DGG, DRG, DGT).");
        if (TiposExcluidos.Contains(tipoDev))
            throw new ArgumentException($"El tipo {tipoDev} está excluido de la vista Pagos (mismo filtro que la sincronización).");
        if (vm.NroDev <= 0)
            throw new ArgumentException("El número de devengado es obligatorio y debe ser mayor a cero.");
        if (vm.FechaDevengado is null)
            throw new ArgumentException("La fecha de devengado es obligatoria.");
        if (vm.Importe is not > 0)
            throw new ArgumentException("El importe es obligatorio y debe ser mayor a cero (mismo filtro que la sincronización).");

        var expediente = Application.Common.ExpedienteKey.Normalizar(vm.Expediente)
            ?? throw new ArgumentException(
                $"Expediente inválido: \"{vm.Expediente}\". Formatos aceptados: {Application.Common.ExpedienteKey.FormatosAceptados}.");

        if (await devengadoRepo.ExistsExactoAsync(tipoDev, vm.NroDev, vm.FechaDevengado, vm.Importe, ct))
            throw new ArgumentException(
                $"Ya existe una fila idéntica del devengado {tipoDev} {vm.NroDev} (misma fecha e importe).");

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

    public Task DeleteDevengadoAsync(int devengadoId, CancellationToken ct = default)
        => devengadoRepo.DeleteAsync(devengadoId, ct);

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
