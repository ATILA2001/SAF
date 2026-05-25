#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Repositories.Abstractions;
using SAF.Services.Abstractions;
using SAF.ViewModels.Pagos;

namespace SAF.Services.Implementations;

public class PagosService(
    IvcDbContext ivcDb,
    IDevengadoExtraRepository extraRepo) : IPagosService
{
    public async Task<IReadOnlyList<PagoViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var devengados = await ivcDb.Devengados
            .OrderBy(d => d.TipoDev)
            .ThenBy(d => d.NroDev)
            .ToListAsync(ct);

        var extras = await extraRepo.GetAllAsync(ct);
        var extrasDict = extras.ToDictionary(e => (e.TipoDev, e.NroDev));

        var result = new List<PagoViewModel>(devengados.Count);
        foreach (var d in devengados)
        {
            extrasDict.TryGetValue((d.TipoDev, d.NroDev), out var extra);
            result.Add(new PagoViewModel
            {
                TipoDev = d.TipoDev,
                NroDev = d.NroDev,
                FechaDevengado = d.FechaDevengado,
                Expediente = d.Expediente,
                Empresa = d.Empresa,
                Importe = d.Importe,
                StatusDgayfOpcionId = extra?.StatusDgayfOpcionId,
                StatusDgayfNombre = extra?.StatusDgayfOpcion?.Nombre,
                StatusOpOpcionId = extra?.StatusOpOpcionId,
                StatusOpNombre = extra?.StatusOpOpcion?.Nombre,
                FechaFirmaOp = extra?.FechaFirmaOp,
                Observaciones = extra?.Observaciones,
                Ccoo = extra?.Ccoo,
                FechaCcoo = extra?.FechaCcoo,
                FechaNotificacion = extra?.FechaNotificacion,
                StatusContable = extra?.StatusContable,
                SegurosTeso = extra?.SegurosTeso,
                FechaDePagoNoCaf = extra?.FechaDePagoNoCaf,
                FechaDePagoCaf = extra?.FechaDePagoCaf,
                FechaPagoTotal = extra?.FechaPagoTotal,
                FechaSade = extra?.FechaSade,
                BuzonSade = extra?.BuzonSade,
                PedidoFactura2 = extra?.PedidoFactura2,
                PedidoFactura3 = extra?.PedidoFactura3,
                FechaFacturaCorrecta = extra?.FechaFacturaCorrecta,
                CafSiNo = extra?.CafSiNo,
            });
        }
        return result;
    }

    public async Task<PagoViewModel?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default)
    {
        var d = await ivcDb.Devengados
            .FirstOrDefaultAsync(x => x.TipoDev == tipoDev && x.NroDev == nroDev, ct);
        if (d is null) return null;

        var extra = await extraRepo.GetByKeyAsync(tipoDev, nroDev, ct);
        return new PagoViewModel
        {
            TipoDev = d.TipoDev,
            NroDev = d.NroDev,
            FechaDevengado = d.FechaDevengado,
            Expediente = d.Expediente,
            Empresa = d.Empresa,
            Importe = d.Importe,
            StatusDgayfOpcionId = extra?.StatusDgayfOpcionId,
            StatusDgayfNombre = extra?.StatusDgayfOpcion?.Nombre,
            StatusOpOpcionId = extra?.StatusOpOpcionId,
            StatusOpNombre = extra?.StatusOpOpcion?.Nombre,
            FechaFirmaOp = extra?.FechaFirmaOp,
            Observaciones = extra?.Observaciones,
            Ccoo = extra?.Ccoo,
            FechaCcoo = extra?.FechaCcoo,
            FechaNotificacion = extra?.FechaNotificacion,
            StatusContable = extra?.StatusContable,
            SegurosTeso = extra?.SegurosTeso,
            FechaDePagoNoCaf = extra?.FechaDePagoNoCaf,
            FechaDePagoCaf = extra?.FechaDePagoCaf,
            FechaPagoTotal = extra?.FechaPagoTotal,
            FechaSade = extra?.FechaSade,
            BuzonSade = extra?.BuzonSade,
            PedidoFactura2 = extra?.PedidoFactura2,
            PedidoFactura3 = extra?.PedidoFactura3,
            FechaFacturaCorrecta = extra?.FechaFacturaCorrecta,
            CafSiNo = extra?.CafSiNo,
        };
    }

    public async Task UpsertAsync(PagoViewModel vm, CancellationToken ct = default)
    {
        var entity = new SAF.Data.Entities.DevengadoExtra
        {
            TipoDev = vm.TipoDev,
            NroDev = vm.NroDev,
            StatusDgayfOpcionId = vm.StatusDgayfOpcionId,
            StatusOpOpcionId = vm.StatusOpOpcionId,
            FechaFirmaOp = vm.FechaFirmaOp,
            Observaciones = vm.Observaciones,
            Ccoo = vm.Ccoo,
            FechaCcoo = vm.FechaCcoo,
            FechaNotificacion = vm.FechaNotificacion,
            StatusContable = vm.StatusContable,
            SegurosTeso = vm.SegurosTeso,
            FechaDePagoNoCaf = vm.FechaDePagoNoCaf,
            FechaDePagoCaf = vm.FechaDePagoCaf,
            FechaPagoTotal = vm.FechaPagoTotal,
            FechaSade = vm.FechaSade,
            BuzonSade = vm.BuzonSade,
            PedidoFactura2 = vm.PedidoFactura2,
            PedidoFactura3 = vm.PedidoFactura3,
            FechaFacturaCorrecta = vm.FechaFacturaCorrecta,
            CafSiNo = vm.CafSiNo,
        };
        await extraRepo.UpsertAsync(entity, ct);
    }
}
