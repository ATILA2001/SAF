#nullable enable
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

        // Una fila por (TipoDev, NroDev): datos de la fila representante + IMPORTE = suma
        // de todas las filas del devengado (en Pagos se ven separadas; acá agrupadas).
        // Representante = la de mayor importe, que es la línea del neto (las retenciones
        // son montos menores); desempate por Id para que no dependa del orden que
        // devuelva SQL. De ella salen expediente, empresa, fecha y los datos cargados.
        var grouped = devengados
            .GroupBy(d => (d.TipoDev, d.NroDev))
            .ToDictionary(
                g => g.Key,
                g => Agrupar(g));

        // BUZÓN SADE / ÚLTIMO MOVIMIENTO (derivadas de IVC.PASES_SADE) quedan vacías en
        // esta fase y las rellena CompletarIvcAsync con la grilla ya visible: la primera
        // conexión a IVC puede tardar segundos y no debe frenar la primera pintada.
        var (pagosDict, extrasDict) = await LoadPropiasAsync();

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

            result.Add(new StatusContabilidadViewModel
            {
                TipoDev = d.TipoDev,
                NroDev = d.NroDev,
                Expediente = d.Expediente,
                Empresa = d.Empresa,
                ImporteTotal = importeTotal,
                CantidadLineas = kvp.Value.Ordenadas.Count,
                DetalleLineas = CompararLineas(kvp.Value.Ordenadas, pagosDict),
                StatusDgayfNombre = pago?.StatusDgayfOpcion?.Nombre,
                FirmadaPorMiguel = pago?.StatusOpOpcion?.Nombre,
                FechaPedidoFactura1 = d.FechaImputacion,
                EeSade = BuildEeSade(d.Expediente),
                Observaciones = pago?.Observaciones,
                Ccoo = pago?.Ccoo,
                FechaCcoo = pago?.FechaCcoo,
                FechaNotificacion = pago?.FechaNotificacion,
                // BuzonSade y UltimoMovimientoSade los rellena CompletarIvcAsync.
                FechaPedidoFactura2 = extra?.FechaPedidoFactura2,
                ReiterarPedidoFactura3 = extra?.ReiterarPedidoFactura3,
                FechaRechazo = extra?.FechaRechazo,
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
                RowVersion = extra?.RowVersion,
            });
        }
        return result;
    }

    public async Task CompletarIvcAsync(IReadOnlyList<StatusContabilidadViewModel> items, CancellationToken ct = default)
    {
        var expedientes = items
            .Where(v => !string.IsNullOrWhiteSpace(v.Expediente))
            .Select(v => v.Expediente!)
            .ToList();
        if (expedientes.Count == 0) return;

        var sade = await sadeRepo.GetByExpedientesAsync(expedientes, ct);

        foreach (var v in items)
        {
            Data.Entities.PaseSade? pase = v.Expediente is not null
                && sade.TryGetValue(v.Expediente, out var ps) ? ps : null;
            v.BuzonSade = pase?.BuzonDestino;
            v.UltimoMovimientoSade = pase?.FechaUltimoPase; // DiasEnElArea deriva de acá
        }
    }

    /// <summary>
    /// Resume las líneas de un devengado: importe sumado, y como representante la de mayor
    /// importe (el neto) con desempate por Id. Sin ese orden explícito el tablero podía
    /// mostrar un expediente distinto entre dos cargas.
    /// </summary>
    private static (Data.Entities.Devengado Fila, decimal ImporteTotal, List<Data.Entities.Devengado> Ordenadas)
        Agrupar(IEnumerable<Data.Entities.Devengado> filas)
    {
        var ordenadas = filas
            .OrderByDescending(d => d.ImportePp ?? decimal.MinValue)
            .ThenBy(d => d.Id)
            .ToList();

        return (ordenadas[0], ordenadas.Sum(d => d.ImportePp ?? 0m), ordenadas);
    }

    // Columnas del tablero que se alimentan de Pagos, en el orden en que se ven.
    private const string ColExpediente = "Expediente";
    private const string ColEmpresa = "Empresa";
    private const string ColImporte = "Importe";
    private const string ColStatusDgayf = "Status DGAyF";
    private const string ColFirmada = "Firmada por Miguel?";
    private const string ColFechaFactura1 = "Fecha Ped. Factura 1";

    private static readonly string[] ColumnasComparables =
        [ColExpediente, ColEmpresa, ColImporte, ColStatusDgayf, ColFirmada, ColFechaFactura1];

    /// <summary>
    /// Compara las líneas del devengado y devuelve solo las columnas en las que difieren:
    /// Tipo y Nro son la clave del grupo (nunca cambian) y repetir lo idéntico es ruido.
    /// </summary>
    private static DetalleLineasViewModel CompararLineas(
        List<Data.Entities.Devengado> ordenadas,
        Dictionary<int, Data.Entities.DevengadoExtra> pagosPorLinea)
    {
        var valores = ordenadas.Select(d =>
        {
            pagosPorLinea.TryGetValue(d.Id, out var pago);
            return new Dictionary<string, string>
            {
                [ColExpediente] = d.Expediente ?? string.Empty,
                [ColEmpresa] = d.Empresa ?? string.Empty,
                [ColImporte] = d.ImportePp?.ToString("N2") ?? string.Empty,
                [ColStatusDgayf] = pago?.StatusDgayfOpcion?.Nombre ?? string.Empty,
                [ColFirmada] = pago?.StatusOpOpcion?.Nombre ?? string.Empty,
                [ColFechaFactura1] = d.FechaImputacion?.ToString("dd/MM/yyyy") ?? string.Empty,
            };
        }).ToList();

        var columnas = ColumnasComparables
            .Where(c => valores.Select(v => v[c]).Distinct().Count() > 1)
            .ToList();

        return new DetalleLineasViewModel
        {
            Columnas = columnas,
            Filas = valores.Select((v, i) => new FilaLineaViewModel
            {
                EsPrincipal = i == 0,
                Valores = columnas.Select(c => string.IsNullOrWhiteSpace(v[c]) ? "—" : v[c]).ToList(),
            }).ToList(),
        };
    }

    public async Task UpsertAsync(StatusContabilidadViewModel vm, CancellationToken ct = default)
    {
        var errores = Application.StatusContabilidad.StatusContabilidadValidator.Validar(vm);
        if (errores.Count > 0) throw new ArgumentException(string.Join(" ", errores));

        var entity = new SAF.Data.Entities.StatusContabilidadExtra
        {
            TipoDev = vm.TipoDev,
            NroDev = vm.NroDev,
            FechaPedidoFactura2 = vm.FechaPedidoFactura2,
            ReiterarPedidoFactura3 = vm.ReiterarPedidoFactura3,
            FechaRechazo = vm.FechaRechazo,
            FechaIngresoFactura = vm.FechaIngresoFactura,
            SinFacturaMotivo = vm.SinFacturaMotivo,
            StatusContableOpcionId = vm.StatusContableOpcionId,
            ObservacionesCuentasPagar = vm.ObservacionesCuentasPagar,
            TramitadorCuentasPagarOpcionId = vm.TramitadorCuentasPagarOpcionId,
            TramitadorLiquidacionesOpcionId = vm.TramitadorLiquidacionesOpcionId,
            ObservacionesLiquidaciones = vm.ObservacionesLiquidaciones,
            // BuzonSade / UltimoMovimientoSade / DiasEnElArea: derivadas de PASES_SADE, no se persisten.
            // Versión que tenía el registro al cargarse: la exige el upsert para detectar
            // que otro usuario lo modificó mientras esta fila estaba en edición.
            RowVersion = vm.RowVersion,
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
