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

        // BUZÓN SADE / ÚLTIMO MOVIMIENTO (derivadas de IVC.PASES_SADE) quedan vacías en
        // esta fase y las rellena CompletarIvcAsync con la grilla ya visible: la primera
        // conexión a IVC puede tardar segundos y no debe frenar la primera pintada.
        var (pagosDict, extrasDict) = await LoadPropiasAsync();

        // Una fila por (TipoDev, NroDev): datos de la línea representante + IMPORTE = suma
        // de las líneas del devengado, con las anuladas en Pagos en cero (en Pagos se ven
        // separadas; acá agrupadas; ver ImporteEnTablero). Representante = la de mayor
        // importe no anulada (ver Agrupar). Se agrupa después de cargar Pagos porque la
        // elección depende del Status DGAyF de cada línea.
        var grouped = devengados
            .GroupBy(d => (d.TipoDev, d.NroDev))
            .ToDictionary(
                g => g.Key,
                g => Agrupar(g, pagosDict));

        // Extras de Pagos: uno por fila del ledger; el tablero usa el de la representante.
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
            result.Add(Mapear(kvp.Value, pagosDict, extrasDict));

        return result;
    }

    public async Task<StatusContabilidadViewModel?> GetPorDevengadoAsync(
        string tipoDev, int nroDev, CancellationToken ct = default)
    {
        var lineas = await devengadoRepo.GetByClaveAsync(tipoDev, nroDev, ct);
        if (lineas.Count == 0) return null;

        // Datos manuales por clave, no las tablas enteras: crecen una fila por cada
        // fila editada, así que escanearlas degradaría cada guardado con el uso. En
        // paralelo: cada repositorio crea su propio DbContext (IDbContextFactory).
        var pagosTask = pagosRepo.GetByDevengadoIdsAsync(lineas.Select(l => l.Id).ToList(), ct);
        var extraTask = contaRepo.GetByClaveAsync(tipoDev, nroDev, ct);
        await Task.WhenAll(pagosTask, extraTask);

        var extrasDict = new Dictionary<(string, int), Data.Entities.StatusContabilidadExtra>();
        if (extraTask.Result is { } extra)
            extrasDict[(extra.TipoDev, extra.NroDev)] = extra;

        var pagosDict = pagosTask.Result.ToDictionary(e => e.DevengadoId);
        return Mapear(Agrupar(lineas, pagosDict), pagosDict, extrasDict);
    }

    /// <summary>
    /// Arma la fila del tablero con el grupo de líneas y los datos manuales. Compartido
    /// por la carga completa y la relectura de una sola fila, para que no se separen.
    /// </summary>
    private static StatusContabilidadViewModel Mapear(
        (Data.Entities.Devengado Fila, List<Data.Entities.Devengado> Ordenadas) grupo,
        Dictionary<int, Data.Entities.DevengadoExtra> pagosDict,
        Dictionary<(string, int), Data.Entities.StatusContabilidadExtra> extrasDict)
    {
        var d = grupo.Fila;
        pagosDict.TryGetValue(d.Id, out var pago);
        extrasDict.TryGetValue((d.TipoDev, d.NroDev), out var extra);
        var (importe, anulado) = ImporteEnTablero(grupo.Ordenadas, pagosDict);

        var vm = new StatusContabilidadViewModel
        {
            TipoDev = d.TipoDev,
            NroDev = d.NroDev,
            Expediente = d.Expediente,
            Empresa = d.Empresa,
            ImporteTotal = importe,
            AnuladoEnPagos = anulado, // antes de CalcularAtraso: un anulado no espera factura
            CantidadLineas = grupo.Ordenadas.Count,
            DetalleLineas = CompararLineas(grupo.Ordenadas, pagosDict),
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
        };
        vm.CalcularAtraso();
        return vm;
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
    /// Ordena las líneas de un devengado y toma como representante la primera: la de
    /// mayor importe que no esté anulada en Pagos. Normalmente es el neto (las retenciones
    /// son montos menores); si el neto está anulado, la que le sigue en importe, y así.
    /// Si todas están anuladas, la de mayor importe. Desempate por Id: sin un orden
    /// explícito el tablero podía mostrar un expediente distinto entre dos cargas.
    ///
    /// Se descarta la anulada para que la fila no muestre "anulado" con un importe que
    /// suma lo vigente: Status DGAyF, Status OP, expediente, empresa, fecha y los datos
    /// cargados en Pagos (observaciones, CCOO, notificación) salen de una línea que sí
    /// cuenta. Consecuencia: anular el neto cambia de qué línea salen esos datos, y lo
    /// cargado en Pagos solo sobre el neto deja de verse hasta que se revierta.
    ///
    /// Las vigentes van primero y las anuladas al final, cada grupo por importe: así el
    /// detalle de líneas marca como principal la primera y lista lo anulado debajo.
    /// </summary>
    private static (Data.Entities.Devengado Fila, List<Data.Entities.Devengado> Ordenadas)
        Agrupar(
            IEnumerable<Data.Entities.Devengado> filas,
            Dictionary<int, Data.Entities.DevengadoExtra> pagosPorLinea)
    {
        var ordenadas = filas
            .OrderBy(d => EstaAnulada(d, pagosPorLinea))
            .ThenByDescending(d => d.ImportePp ?? decimal.MinValue)
            .ThenBy(d => d.Id)
            .ToList();

        return (ordenadas[0], ordenadas);
    }

    private static bool EstaAnulada(
        Data.Entities.Devengado linea,
        Dictionary<int, Data.Entities.DevengadoExtra> pagosPorLinea)
        => pagosPorLinea.TryGetValue(linea.Id, out var pago)
           && Application.Pagos.ReglasPago.EsAnulado(pago.StatusDgayfOpcion?.Nombre);

    /// <summary>
    /// Importe del devengado en el tablero: la suma de sus líneas, sin las que tienen
    /// Status DGAyF "anulado" en Pagos. Anulado = todas sus líneas lo están.
    ///
    /// Por línea y no por devengado, porque el status se carga en cada línea (neto y
    /// retenciones): todas anuladas da cero y, si solo algunas, suma lo que sigue
    /// vigente. Se descartó poner cero cuando la línea principal está anulada: es más
    /// simple, pero esconde las líneas que no lo están. Si se anula solo el neto, la
    /// fila toma los datos de la retención de mayor importe (ver Agrupar) y el detalle
    /// de líneas muestra el neto anulado al final.
    ///
    /// Se calcula al leer y no se guarda en el ledger: la sync y el diff de expedientes
    /// reconocen cada línea por tipo, número, fecha e importe, así que con el importe
    /// pisado la línea dejaría de reconocerse contra IVC (y si es del último día, la sync
    /// la importaría de nuevo, duplicada). Así, además, sacar el "anulado" devuelve el
    /// importe solo.
    ///
    /// Solo cuenta el status de Pagos: las marcas "Anulado" del propio tablero (Status
    /// Contable y Sin factura) son seguimiento de la factura y no tocan el importe.
    /// </summary>
    private static (decimal Importe, bool Anulado) ImporteEnTablero(
        List<Data.Entities.Devengado> lineas,
        Dictionary<int, Data.Entities.DevengadoExtra> pagosPorLinea)
    {
        var vigentes = lineas.Where(l => !EstaAnulada(l, pagosPorLinea)).ToList();

        // Las líneas nunca vienen vacías (el grupo sale de al menos una fila del ledger).
        return (vigentes.Sum(l => l.ImportePp ?? 0m), vigentes.Count == 0);
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
                // El importe original también en las anuladas: junto al Status DGAyF de
                // cada línea explica por qué el total de la fila suma menos.
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
