#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;
using SAF.Application.StatusContabilidad.Dtos;

namespace SAF.Services.Implementations;

public class StatusContabilidadService(
    IDevengadoRepository devengadoRepo,
    IDevengadoExtraRepository pagosRepo,
    IStatusContabilidadExtraRepository contaRepo,
    ISadeRepository sadeRepo) : IStatusContabilidadService
{
    public async Task<IReadOnlyList<StatusContabilidadViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var devengados = await devengadoRepo.GetAllAsync(ct);

        // Una fila por (TipoDev, NroDev): datos de la primera fila + IMPORTE = suma de
        // todas las filas del devengado (en Pagos se ven separadas; acá agrupadas).
        var grouped = devengados
            .GroupBy(d => (d.TipoDev, d.NroDev))
            .ToDictionary(
                g => g.Key,
                g => (Fila: g.First(), ImporteTotal: g.Sum(x => x.ImportePp ?? 0m)));

        // BUZÓN SADE / ÚLTIMO MOVIMIENTO: VLOOKUP a la hoja SADE en el Excel → derivadas
        // de IVC.PASES_SADE filtrando por los expedientes de la grilla (la tabla IVC tiene
        // ~53k expedientes; la grilla ~1,6k). Corre en paralelo con las de las tablas
        // propias: cada repositorio crea su propio DbContext (IDbContextFactory).
        var expedientes = grouped.Values
            .Where(v => !string.IsNullOrWhiteSpace(v.Fila.Expediente))
            .Select(v => v.Fila.Expediente!)
            .ToList();
        var sadeTask = sadeRepo.GetByExpedientesAsync(expedientes, ct);
        var propiasTask = LoadPropiasAsync();
        await Task.WhenAll(sadeTask, propiasTask);
        var sade = sadeTask.Result;
        var (pagosDict, extrasDict) = propiasTask.Result;

        // Extras de Pagos: uno por fila del ledger; el tablero usa el de la primera fila del grupo.
        async Task<(Dictionary<int, Data.Entities.DevengadoExtra>,
                    Dictionary<(string, int), Data.Entities.StatusContabilidadExtra>)> LoadPropiasAsync()
        {
            var pagos = await pagosRepo.GetAllAsync(ct);
            var extras = await contaRepo.GetAllAsync(ct);
            return (pagos.ToDictionary(e => e.DevengadoId),
                    extras.ToDictionary(e => ((string)e.TipoDev, e.NroDev)));
        }

        var result = new List<StatusContabilidadViewModel>(grouped.Count);
        foreach (var kvp in grouped)
        {
            var d = kvp.Value.Fila;
            var importeTotal = kvp.Value.ImporteTotal;
            var key = kvp.Key;
            pagosDict.TryGetValue(d.Id, out var pago);
            extrasDict.TryGetValue(key, out var extra);

            Data.Entities.PaseSade? pase = d.Expediente is not null
                && sade.TryGetValue(d.Expediente, out var ps) ? ps : null;

            result.Add(new StatusContabilidadViewModel
            {
                TipoDev = d.TipoDev,
                NroDev = d.NroDev,
                Expediente = d.Expediente,
                Empresa = d.Empresa,
                ImporteTotal = importeTotal,
                StatusDgayfNombre = pago?.StatusDgayfOpcion?.Nombre,
                FirmadaPorMiguel = pago?.StatusOpOpcion?.Nombre,
                FechaPedidoFactura1 = d.FechaImputacion,
                EeSade = BuildEeSade(d.Expediente),
                Observaciones = pago?.Observaciones,
                Ccoo = pago?.Ccoo,
                FechaCcoo = pago?.FechaCcoo,
                FechaNotificacion = pago?.FechaNotificacion,
                BuzonSade = pase?.BuzonDestino,
                FechaPedidoFactura2 = extra?.FechaPedidoFactura2,
                ReiterarPedidoFactura3 = extra?.ReiterarPedidoFactura3,
                FechaIngresoFactura = extra?.FechaIngresoFactura,
                SinFacturaMotivo = extra?.SinFacturaMotivo,
                StatusContableOpcionId = extra?.StatusContableOpcionId,
                StatusContableNombre = extra?.StatusContableOpcion?.Nombre,
                ObservacionesCuentasPagar = extra?.ObservacionesCuentasPagar,
                TramitadorCuentasPagarOpcionId = extra?.TramitadorCuentasPagarOpcionId,
                TramitadorCuentasPagarNombre = extra?.TramitadorCuentasPagarOpcion?.Nombre,
                TramitadorLiquidacionesOpcionId = extra?.TramitadorLiquidacionesOpcionId,
                TramitadorLiquidacionesNombre = extra?.TramitadorLiquidacionesOpcion?.Nombre,
                ObservacionesLiquidaciones = extra?.ObservacionesLiquidaciones,
                FaltaPoliza = extra?.FaltaPoliza ?? false,
                UltimoMovimientoSade = pase?.FechaUltimoPase,
            });
        }
        return result;
    }

    public async Task<StatusContabilidadViewModel?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default)
    {
        var d = await devengadoRepo.GetByKeyAsync(tipoDev, nroDev, ct);
        if (d is null) return null;

        var pago = await pagosRepo.GetByDevengadoIdAsync(d.Id, ct);
        var extra = await contaRepo.GetByKeyAsync(tipoDev, nroDev, ct);

        Data.Entities.PaseSade? pase = null;
        if (!string.IsNullOrWhiteSpace(d.Expediente))
        {
            var sade = await sadeRepo.GetByExpedientesAsync(new[] { d.Expediente }, ct);
            sade.TryGetValue(d.Expediente, out pase);
        }

        return new StatusContabilidadViewModel
        {
            TipoDev = d.TipoDev,
            NroDev = d.NroDev,
            Expediente = d.Expediente,
            Empresa = d.Empresa,
            // Misma semántica que la grilla: suma de todas las filas del devengado.
            ImporteTotal = await devengadoRepo.GetSumImportePpByKeyAsync(tipoDev, nroDev, ct),
            StatusDgayfNombre = pago?.StatusDgayfOpcion?.Nombre,
            FirmadaPorMiguel = pago?.StatusOpOpcion?.Nombre,
            FechaPedidoFactura1 = d.FechaImputacion,
            EeSade = BuildEeSade(d.Expediente),
            Observaciones = pago?.Observaciones,
            Ccoo = pago?.Ccoo,
            FechaCcoo = pago?.FechaCcoo,
            FechaNotificacion = pago?.FechaNotificacion,
            BuzonSade = pase?.BuzonDestino,
            FechaPedidoFactura2 = extra?.FechaPedidoFactura2,
            ReiterarPedidoFactura3 = extra?.ReiterarPedidoFactura3,
            FechaIngresoFactura = extra?.FechaIngresoFactura,
            StatusContableOpcionId = extra?.StatusContableOpcionId,
            StatusContableNombre = extra?.StatusContableOpcion?.Nombre,
            ObservacionesCuentasPagar = extra?.ObservacionesCuentasPagar,
            TramitadorCuentasPagarOpcionId = extra?.TramitadorCuentasPagarOpcionId,
            TramitadorCuentasPagarNombre = extra?.TramitadorCuentasPagarOpcion?.Nombre,
            TramitadorLiquidacionesOpcionId = extra?.TramitadorLiquidacionesOpcionId,
            TramitadorLiquidacionesNombre = extra?.TramitadorLiquidacionesOpcion?.Nombre,
            ObservacionesLiquidaciones = extra?.ObservacionesLiquidaciones,
            FaltaPoliza = extra?.FaltaPoliza ?? false,
            UltimoMovimientoSade = pase?.FechaUltimoPase,
        };
    }

    public async Task UpsertAsync(StatusContabilidadViewModel vm, CancellationToken ct = default)
    {
        var entity = new SAF.Data.Entities.StatusContabilidadExtra
        {
            TipoDev = vm.TipoDev,
            NroDev = vm.NroDev,
            FechaPedidoFactura2 = vm.FechaPedidoFactura2,
            ReiterarPedidoFactura3 = vm.ReiterarPedidoFactura3,
            FechaIngresoFactura = vm.FechaIngresoFactura,
            SinFacturaMotivo = vm.SinFacturaMotivo,
            StatusContableOpcionId = vm.StatusContableOpcionId,
            ObservacionesCuentasPagar = vm.ObservacionesCuentasPagar,
            TramitadorCuentasPagarOpcionId = vm.TramitadorCuentasPagarOpcionId,
            TramitadorLiquidacionesOpcionId = vm.TramitadorLiquidacionesOpcionId,
            ObservacionesLiquidaciones = vm.ObservacionesLiquidaciones,
            FaltaPoliza = vm.FaltaPoliza,
            // BuzonSade / UltimoMovimientoSade / DiasEnElArea: derivadas de PASES_SADE, no se persisten.
        };
        await contaRepo.UpsertAsync(entity, ct);
    }

    /// <summary>
    /// Construye el EE SADE a partir del expediente.
    /// Ejemplo: "27223878/24" → "EX-2024-27223878-GCABA-IVC"
    /// </summary>
    private static string? BuildEeSade(string? expediente)
    {
        if (string.IsNullOrWhiteSpace(expediente)) return null;

        var parts = expediente.Split('/');
        if (parts.Length != 2) return null;

        var numero = parts[0].Trim();
        var anioCorto = parts[1].Trim();
        if (!int.TryParse(anioCorto, out var anioNum)) return null;

        var anioCompleto = anioNum < 100 ? 2000 + anioNum : anioNum;
        return $"EX-{anioCompleto}-{numero}-GCABA-IVC";
    }
}
