#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;
using SAF.ViewModels.StatusContabilidad;

namespace SAF.Services.Implementations;

public class StatusContabilidadService(
    IvcDbContext ivcDb,
    IDevengadoExtraRepository pagosRepo,
    IStatusContabilidadExtraRepository contaRepo) : IStatusContabilidadService
{
    public async Task<IReadOnlyList<StatusContabilidadViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var devengados = await ivcDb.Devengados
            .OrderBy(d => d.TipoDev)
            .ThenBy(d => d.NroDev)
            .ToListAsync(ct);

        // Agrupar por (TipoDev, NroDev) y tomar el primero de cada grupo
        var grouped = devengados
            .GroupBy(d => (d.TipoDev, d.NroDev))
            .ToDictionary(g => g.Key, g => g.First());

        var pagos = await pagosRepo.GetAllAsync(ct);
        var pagosDict = pagos.ToDictionary(e => (e.TipoDev, e.NroDev));

        var extras = await contaRepo.GetAllAsync(ct);
        var extrasDict = extras.ToDictionary(e => (e.TipoDev, e.NroDev));

        var result = new List<StatusContabilidadViewModel>(grouped.Count);
        foreach (var kvp in grouped)
        {
            var d = kvp.Value;
            var key = kvp.Key;
            pagosDict.TryGetValue(key, out var pago);
            extrasDict.TryGetValue(key, out var extra);

            var importeTotal = devengados
                .Where(x => x.TipoDev == key.TipoDev && x.NroDev == key.NroDev)
                .Sum(x => x.Importe ?? 0m);

            result.Add(new StatusContabilidadViewModel
            {
                TipoDev = d.TipoDev,
                NroDev = d.NroDev,
                Expediente = d.Expediente,
                Empresa = d.Empresa,
                ImporteTotal = importeTotal,
                StatusDgayfNombre = pago?.StatusDgayfOpcion?.Nombre,
                FirmadaPorMiguel = pago?.StatusOpOpcion?.Nombre,
                FechaPedidoFactura1 = d.FechaDevengado,
                EeSade = BuildEeSade(d.Expediente),
                Observaciones = pago?.Observaciones,
                Ccoo = pago?.Ccoo,
                FechaCcoo = pago?.FechaCcoo,
                FechaNotificacion = pago?.FechaNotificacion,
                BuzonSade = pago?.BuzonSade,
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
                UltimoMovimientoSade = extra?.UltimoMovimientoSade,
            });
        }
        return result;
    }

    public async Task<StatusContabilidadViewModel?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default)
    {
        var d = await ivcDb.Devengados
            .FirstOrDefaultAsync(x => x.TipoDev == tipoDev && x.NroDev == nroDev, ct);
        if (d is null) return null;

        var pago = await pagosRepo.GetByKeyAsync(tipoDev, nroDev, ct);
        var extra = await contaRepo.GetByKeyAsync(tipoDev, nroDev, ct);

        return new StatusContabilidadViewModel
        {
            TipoDev = d.TipoDev,
            NroDev = d.NroDev,
            Expediente = d.Expediente,
            Empresa = d.Empresa,
            ImporteTotal = d.Importe,
            StatusDgayfNombre = pago?.StatusDgayfOpcion?.Nombre,
            FirmadaPorMiguel = pago?.StatusOpOpcion?.Nombre,
            FechaPedidoFactura1 = d.FechaDevengado,
            EeSade = BuildEeSade(d.Expediente),
            Observaciones = pago?.Observaciones,
            Ccoo = pago?.Ccoo,
            FechaCcoo = pago?.FechaCcoo,
            FechaNotificacion = pago?.FechaNotificacion,
            BuzonSade = pago?.BuzonSade,
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
            UltimoMovimientoSade = extra?.UltimoMovimientoSade,
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
            StatusContableOpcionId = vm.StatusContableOpcionId,
            ObservacionesCuentasPagar = vm.ObservacionesCuentasPagar,
            TramitadorCuentasPagarOpcionId = vm.TramitadorCuentasPagarOpcionId,
            TramitadorLiquidacionesOpcionId = vm.TramitadorLiquidacionesOpcionId,
            ObservacionesLiquidaciones = vm.ObservacionesLiquidaciones,
            FaltaPoliza = vm.FaltaPoliza,
            UltimoMovimientoSade = vm.UltimoMovimientoSade,
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
