#nullable enable
// Migración one-off de las planillas históricas a la base de SAF.
//
// Uso (desde la raíz del repo):
//   dotnet run --project Tools/MigracionExcel -- ^
//     --pagos   "...\Pagos 2026.xlsx"               (hoja PAGOS  → Devengados + DevengadosExtra)
//     --tablero "...\Status Contabilidad 2026.xlsx" (hoja Tablero Contable → StatusContabilidadExtras)
//     --caf     "...\EXPEDIENTES CAF - 2026.xlsx"   (hoja CAF-2026 → ExpedientesCaf)
//     --seguros "...\PAGOS PENDIENTES-.xlsx"        (hoja Pagos → ExpedientesSeguro)
//     [--aplicar] [--reporte salida.txt]
//
// Sin --aplicar corre en modo dry-run: lee, valida y reporta sin escribir nada.
// Es idempotente: lo que ya existe en la base se omite (los devengados con el mismo
// diff por multiplicidad que usa DevengadoSyncService; el resto por clave de negocio).

using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SAF.Application.Common;
using SAF.Data;
using SAF.Data.Entities;

// ---------------- CLI ----------------
string? rutaPagos = null, rutaTablero = null, rutaCaf = null, rutaSeguros = null, rutaReporte = null;
bool aplicar = false, permitirProduccion = false, mostrarListas = false;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--pagos": rutaPagos = args[++i]; break;
        case "--tablero": rutaTablero = args[++i]; break;
        case "--caf": rutaCaf = args[++i]; break;
        case "--seguros": rutaSeguros = args[++i]; break;
        case "--reporte": rutaReporte = args[++i]; break;
        case "--aplicar": aplicar = true; break;
        case "--permitir-produccion": permitirProduccion = true; break;
        case "--mostrar-listas": mostrarListas = true; break;
        default:
            Console.Error.WriteLine($"Argumento desconocido: {args[i]}");
            return 1;
    }
}

if (!mostrarListas && rutaPagos is null && rutaTablero is null && rutaCaf is null && rutaSeguros is null)
{
    Console.Error.WriteLine("Nada que importar: indicá al menos uno de --pagos / --tablero / --caf / --seguros.");
    return 1;
}
foreach (var (nombre, ruta) in new[] { ("--pagos", rutaPagos), ("--tablero", rutaTablero), ("--caf", rutaCaf), ("--seguros", rutaSeguros) })
{
    if (ruta is not null && !File.Exists(ruta))
    {
        Console.Error.WriteLine($"No existe el archivo de {nombre}: {ruta}");
        return 1;
    }
}

// ---------------- Configuración (misma que la app: appsettings + user secrets) ----------------
var raiz = BuscarRaizRepo();
var config = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(raiz, "appsettings.json"), optional: true)
    .AddJsonFile(Path.Combine(raiz, "appsettings.Development.json"), optional: true)
    .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true)
    .AddEnvironmentVariables()
    .Build();

var cadena = config.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cadena) || cadena.Contains("YOUR_SERVER"))
{
    Console.Error.WriteLine("DefaultConnection no está configurada (appsettings / user secrets).");
    return 1;
}

var csb = new SqlConnectionStringBuilder(cadena);
Console.WriteLine($"Base destino: servidor [{csb.DataSource}], base [{csb.InitialCatalog}]");
Console.WriteLine(aplicar ? "Modo: APLICAR (escribe en la base)" : "Modo: dry-run (no escribe nada)");
Console.WriteLine();

// El servidor productivo no se toca sin autorización explícita.
if (aplicar && csb.DataSource.Contains("10.10.12.37") && !permitirProduccion)
{
    Console.Error.WriteLine("La conexión apunta al servidor productivo (10.10.12.37). " +
                            "Para escribir ahí hace falta el flag --permitir-produccion.");
    return 1;
}

var opciones = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cadena).Options;
await using var db = new AppDbContext(opciones);

var rep = new Reporte();
var esAr = CultureInfo.GetCultureInfo("es-AR");

// Lookups de la base (por nombre, case-insensitive)
var statusDgayf = await CargarLookup(db.StatusDgayfOpciones.Select(o => new { o.Id, o.Nombre }).ToListAsync(), x => x.Nombre, x => x.Id);
var statusOp = await CargarLookup(db.StatusOpOpciones.Select(o => new { o.Id, o.Nombre }).ToListAsync(), x => x.Nombre, x => x.Id);
var statusContable = await CargarLookup(db.StatusContableOpciones.Select(o => new { o.Id, o.Nombre }).ToListAsync(), x => x.Nombre, x => x.Id);
var tramCp = await CargarLookup(db.TramitadoresCuentasPagar.Select(o => new { o.Id, o.Nombre }).ToListAsync(), x => x.Nombre, x => x.Id);
var tramLiq = await CargarLookup(db.TramitadoresLiquidaciones.Select(o => new { o.Id, o.Nombre }).ToListAsync(), x => x.Nombre, x => x.Id);
var seguroOpc = await CargarLookup(db.SeguroOpciones.Select(o => new { o.Id, o.Nombre }).ToListAsync(), x => x.Nombre, x => x.Id);

static async Task<Dictionary<string, int>> CargarLookup<T>(Task<List<T>> tarea, Func<T, string> nombre, Func<T, int> id)
{
    var lista = await tarea;
    var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var item in lista)
        dict.TryAdd(nombre(item).Trim(), id(item));
    return dict;
}

if (mostrarListas)
{
    foreach (var (nombre, dict) in new (string, Dictionary<string, int>)[]
             { ("StatusDgayf", statusDgayf), ("StatusOp", statusOp), ("StatusContable", statusContable),
               ("TramitadorCtasPagar", tramCp), ("TramitadorLiquidaciones", tramLiq), ("SeguroOpcion", seguroOpc) })
    {
        Console.WriteLine($"--- {nombre} ({dict.Count}):");
        foreach (var (valor, id) in dict.OrderBy(kv => kv.Value))
            Console.WriteLine($"  [{id}] \"{valor}\" (len {valor.Length}, codes: {string.Join(" ", valor.Select(ch => ((int)ch).ToString("X2")))})");
    }
    return 0;
}

// expediente financiera → claves (TipoDev, NroDev) vistas en la hoja PAGOS.
// Sirve para recuperar la clave en filas del tablero que no la traen.
var clavesPorExpediente = new Dictionary<string, HashSet<(string, int)>>(StringComparer.OrdinalIgnoreCase);

await using var tx = aplicar ? await db.Database.BeginTransactionAsync() : null;

// ============================================================================
// 1) Hoja PAGOS → Devengados (ledger) + DevengadosExtra (campos editables)
// ============================================================================
if (rutaPagos is not null)
{
    Console.WriteLine("Leyendo hoja PAGOS…");
    using var wb = new XLWorkbook(rutaPagos);
    var ws = wb.Worksheet("PAGOS");
    var ultima = ws.RangeUsed()!.LastRow().RowNumber();

    // Diff por multiplicidad contra lo ya sincronizado/cargado (misma regla que
    // DevengadoSyncService): cada línea existente se consume una sola vez.
    var existentes = await db.Devengados.AsNoTracking()
        .Select(d => new { d.Id, d.TipoDev, d.NroDev, d.FechaImputacion, d.ImportePp })
        .ToListAsync();
    var disponibles = existentes
        .GroupBy(e => (e.TipoDev.Trim().ToUpperInvariant(), e.NroDev, e.FechaImputacion, e.ImportePp))
        .ToDictionary(g => g.Key, g => new Queue<int>(g.Select(x => x.Id)));

    var extrasExistentes = (await db.DevengadosExtra.AsNoTracking().Select(e => e.DevengadoId).ToListAsync()).ToHashSet();

    var lineas = new List<(Devengado? Nuevo, int? IdExistente, DevengadoExtra? Extra)>();

    for (int f = 2; f <= ultima; f++)
    {
        var fila = ws.Row(f);
        // Arrastre de fórmulas: las columnas derivadas (N–X) tienen fórmulas hasta el
        // final de la hoja; sin datos propios en A–F la fila no existe.
        if (SinDato(fila, "A", "B", "C", "D", "E", "F")) continue;

        var tipoDev = LeerTexto(fila.Cell("A"));
        var nroDev = LeerEntero(fila.Cell("B"));
        if (tipoDev is null || nroDev is null)
        {
            rep.Rechazo("PAGOS", f, "sin TIPO DEV / NRO DEV (no puede entrar al ledger)");
            continue;
        }
        tipoDev = tipoDev.ToUpperInvariant();

        var fechaDev = LeerFecha(fila.Cell("C"), "PAGOS", f, "FECHA DEVENGADO");
        var expCrudo = LeerTexto(fila.Cell("D"));
        var expediente = ExpedienteKey.Normalizar(expCrudo) ?? expCrudo;   // como el sync: si no normaliza, se conserva crudo
        if (expCrudo is not null && ExpedienteKey.Normalizar(expCrudo) is null)
            rep.NoMapeado("PAGOS: expediente sin normalizar", expCrudo);
        var empresa = Truncar(LeerTexto(fila.Cell("E")), 255, "PAGOS", f, "EMPRESA");
        var importe = LeerDecimal(fila.Cell("F"), "PAGOS", f, "IMPORTE");

        if (expediente is not null)
        {
            if (!clavesPorExpediente.TryGetValue(expediente, out var set))
                clavesPorExpediente[expediente] = set = new();
            set.Add((tipoDev, nroDev.Value));
        }

        // Campos editables → DevengadoExtra (solo si hay algo cargado)
        var dgayfNombre = LeerTexto(fila.Cell("G"));
        var opNombre = LeerTexto(fila.Cell("H"));
        var extra = new DevengadoExtra
        {
            TipoDev = tipoDev,
            NroDev = nroDev.Value,
            StatusDgayfOpcionId = Mapear(statusDgayf, dgayfNombre, "PAGOS: STATUS DGAyF"),
            StatusOpOpcionId = Mapear(statusOp, opNombre, "PAGOS: STATUS OP"),
            FechaFirmaOp = LeerFecha(fila.Cell("I"), "PAGOS", f, "FECHA FIRMA OP"),
            Observaciones = Truncar(LeerTexto(fila.Cell("J")), 500, "PAGOS", f, "OBSERVACIONES"),
            Ccoo = Truncar(LeerTexto(fila.Cell("K")), 200, "PAGOS", f, "CCOO"),
            FechaCcoo = LeerFecha(fila.Cell("L"), "PAGOS", f, "FECHA CCOO"),
            FechaNotificacion = LeerFecha(fila.Cell("M"), "PAGOS", f, "FECHA NOTIFICACION"),
            CafSiNo = LeerCafSiNo(fila.Cell("X")),
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = DateTime.UtcNow,
        };
        bool extraTieneDatos = extra.StatusDgayfOpcionId is not null || extra.StatusOpOpcionId is not null
            || extra.FechaFirmaOp is not null || extra.Observaciones is not null || extra.Ccoo is not null
            || extra.FechaCcoo is not null || extra.FechaNotificacion is not null || extra.CafSiNo is not null;

        var clave = (tipoDev, nroDev.Value, fechaDev, importe);
        if (disponibles.TryGetValue(clave, out var cola) && cola.Count > 0)
        {
            lineas.Add((null, cola.Dequeue(), extraTieneDatos ? extra : null));
            rep.Contar("Devengados", "ya existían (omitidos)");
        }
        else
        {
            var nuevo = new Devengado
            {
                TipoDev = tipoDev,
                NroDev = nroDev.Value,
                FechaImputacion = fechaDev,
                Expediente = expediente,
                Empresa = empresa,
                ImportePp = importe,
                FechaImportacion = DateTime.UtcNow,
            };
            lineas.Add((nuevo, null, extraTieneDatos ? extra : null));
            rep.Contar("Devengados", "a insertar");
        }
    }

    if (aplicar)
    {
        db.Devengados.AddRange(lineas.Where(l => l.Nuevo is not null).Select(l => l.Nuevo!));
        await db.SaveChangesAsync();
    }

    foreach (var (nuevo, idExistente, extra) in lineas)
    {
        if (extra is null) continue;
        var devId = nuevo?.Id ?? idExistente!.Value;
        if (idExistente is not null && extrasExistentes.Contains(idExistente.Value))
        {
            // La app ya tiene datos editables para esa línea: se respetan.
            rep.Contar("DevengadosExtra", "ya tenían datos en la app (omitidos)");
            continue;
        }
        extra.DevengadoId = devId;
        if (aplicar) db.DevengadosExtra.Add(extra);
        rep.Contar("DevengadosExtra", "a insertar");
    }
    if (aplicar) await db.SaveChangesAsync();
}

// ============================================================================
// 2) Hoja Tablero Contable → StatusContabilidadExtras
// ============================================================================
if (rutaTablero is not null)
{
    Console.WriteLine("Leyendo hoja Tablero Contable…");
    using var wb = new XLWorkbook(rutaTablero);
    var ws = wb.Worksheet("Tablero Contable");
    var ultima = ws.RangeUsed()!.LastRow().RowNumber();

    var yaExisten = (await db.StatusContabilidadExtras.AsNoTracking()
        .Select(e => new { e.TipoDev, e.NroDev }).ToListAsync())
        .Select(e => (e.TipoDev.Trim().ToUpperInvariant(), e.NroDev)).ToHashSet();

    // Claves del ledger en la base (para no crear extras huérfanos)
    var clavesLedger = (await db.Devengados.AsNoTracking()
        .Select(d => new { d.TipoDev, d.NroDev }).ToListAsync())
        .Select(d => (d.TipoDev.Trim().ToUpperInvariant(), d.NroDev)).ToHashSet();
    // En dry-run los devengados de la hoja PAGOS todavía no están en la base:
    // se suman sus claves para evaluar el cruce como si ya estuvieran.
    foreach (var set in clavesPorExpediente.Values)
        foreach (var k in set) clavesLedger.Add(k);

    var vistos = new HashSet<(string, int)>();

    for (int f = 3; f <= ultima; f++)   // fila 1: sumas; fila 2: encabezados
    {
        var fila = ws.Row(f);
        if (SinDato(fila, "A", "B", "C", "D", "E")) continue;   // arrastre de fórmulas

        var tipoDev = LeerTexto(fila.Cell("A"))?.ToUpperInvariant();
        var nroDev = LeerEntero(fila.Cell("B"));

        if (tipoDev is null || nroDev is null)
        {
            // Recuperar la clave por expediente (col C) contra lo visto en PAGOS
            var exp = ExpedienteKey.Normalizar(LeerTexto(fila.Cell("C")));
            if (exp is not null && clavesPorExpediente.TryGetValue(exp, out var set) && set.Count == 1)
            {
                (tipoDev, nroDev) = (set.First().Item1, set.First().Item2);
            }
            else
            {
                rep.Rechazo("Tablero Contable", f, $"sin Tipo/Nro Dev y el expediente no permite recuperarlos ({LeerTexto(fila.Cell("C")) ?? "vacío"})");
                continue;
            }
        }

        var clave = (tipoDev, nroDev.Value);
        if (!vistos.Add(clave))
        {
            rep.Rechazo("Tablero Contable", f, $"clave repetida en la planilla ({tipoDev} {nroDev}); se conservó la primera");
            continue;
        }
        if (yaExisten.Contains(clave))
        {
            rep.Contar("StatusContabilidadExtras", "ya tenían datos en la app (omitidos)");
            continue;
        }
        if (!clavesLedger.Contains(clave))
        {
            rep.Rechazo("Tablero Contable", f, $"({tipoDev} {nroDev}) no existe en el ledger de devengados");
            continue;
        }

        // Col K: fecha de ingreso de factura o marca textual (N/C, CCOO, PAV, Anulado…)
        var (fechaIngreso, motivo) = LeerFechaOMotivo(fila.Cell("K"), "Tablero Contable", f, "Fecha de Ingreso Factura");

        var extra = new StatusContabilidadExtra
        {
            TipoDev = tipoDev,
            NroDev = nroDev.Value,
            FechaPedidoFactura2 = LeerFecha(fila.Cell("I"), "Tablero Contable", f, "Fecha pedido factura 2"),
            ReiterarPedidoFactura3 = LeerFecha(fila.Cell("J"), "Tablero Contable", f, "Reiterar pedido factura 3"),
            FechaIngresoFactura = fechaIngreso,
            SinFacturaMotivo = Truncar(motivo, 20, "Tablero Contable", f, "SinFacturaMotivo"),
            StatusContableOpcionId = Mapear(statusContable, LeerTexto(fila.Cell("L")), "Tablero: Status Contable"),
            ObservacionesCuentasPagar = Truncar(LeerTexto(fila.Cell("M")), 500, "Tablero Contable", f, "Obs. Ctas. a Pagar"),
            TramitadorCuentasPagarOpcionId = Mapear(tramCp, LeerTexto(fila.Cell("N")), "Tablero: Tramitador Ctas. a Pagar"),
            TramitadorLiquidacionesOpcionId = Mapear(tramLiq, LeerTexto(fila.Cell("O")), "Tablero: Tramitador Liquidaciones"),
            ObservacionesLiquidaciones = Truncar(LeerTexto(fila.Cell("P")), 500, "Tablero Contable", f, "Obs. Liquidaciones"),
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = DateTime.UtcNow,
        };

        bool tieneDatos = extra.FechaPedidoFactura2 is not null || extra.ReiterarPedidoFactura3 is not null
            || extra.FechaIngresoFactura is not null || extra.SinFacturaMotivo is not null
            || extra.StatusContableOpcionId is not null || extra.ObservacionesCuentasPagar is not null
            || extra.TramitadorCuentasPagarOpcionId is not null || extra.TramitadorLiquidacionesOpcionId is not null
            || extra.ObservacionesLiquidaciones is not null;
        if (!tieneDatos)
        {
            rep.Contar("StatusContabilidadExtras", "sin datos propios (omitidos)");
            continue;
        }

        if (aplicar) db.StatusContabilidadExtras.Add(extra);
        rep.Contar("StatusContabilidadExtras", "a insertar");
    }
    if (aplicar) await db.SaveChangesAsync();
}

// ============================================================================
// 3) Hoja CAF-2026 → ExpedientesCaf
// ============================================================================
if (rutaCaf is not null)
{
    Console.WriteLine("Leyendo hoja CAF-2026…");
    using var wb = new XLWorkbook(rutaCaf);
    var ws = wb.Worksheet("CAF-2026");
    var ultima = ws.RangeUsed()!.LastRow().RowNumber();

    var yaExisten = (await db.ExpedientesCaf.AsNoTracking()
        .Select(e => new { e.Expediente, e.Op }).ToListAsync())
        .Select(e => (e.Expediente, e.Op ?? "")).ToHashSet();

    for (int f = 2; f <= ultima; f++)
    {
        var fila = ws.Row(f);
        // La col A tiene arrastre de fórmula más allá de los datos: sin OP no hay fila real.
        var op = LeerTexto(fila.Cell("C"));
        if (op is null && LeerTexto(fila.Cell("D")) is null && LeerDecimal(fila.Cell("E"), null, f, null) is null)
            continue;

        // Col A viene con el bug de 9 dígitos de la fórmula original; col B trae el formato SADE.
        var expediente = ExpedienteKey.Normalizar(LeerTexto(fila.Cell("A")))
            ?? ExpedienteKey.Normalizar(LeerTexto(fila.Cell("B")))
            ?? NormalizarConCerosDeMas(LeerTexto(fila.Cell("A")));
        if (expediente is null)
        {
            rep.Rechazo("CAF-2026", f, $"expediente inválido ({LeerTexto(fila.Cell("A")) ?? "vacío"} / {LeerTexto(fila.Cell("B")) ?? "vacío"})");
            continue;
        }

        if (!yaExisten.Add((expediente, op ?? "")))
        {
            rep.Contar("ExpedientesCaf", "ya existían (omitidos)");
            continue;
        }

        var caf = new ExpedienteCaf
        {
            Anio = 2026,
            Expediente = expediente,
            Op = Truncar(op, 50, "CAF-2026", f, "OP"),
            Beneficiario = Truncar(LeerTexto(fila.Cell("D")), 255, "CAF-2026", f, "BENEFICIARIO"),
            ImporteNeto = LeerDecimal(fila.Cell("E"), "CAF-2026", f, "IMPORTE NETO"),
            Iibb = LeerDecimal(fila.Cell("F"), "CAF-2026", f, "IIBB"),
            Cuenta = Truncar(LeerTexto(fila.Cell("G")), 50, "CAF-2026", f, "CUENTA"),
            FechaPago = LeerFecha(fila.Cell("H"), "CAF-2026", f, "FECHA PAGO"),
            Cargado = CombinarFechaHora(LeerFecha(fila.Cell("I"), "CAF-2026", f, "CARGADO FECHA"), LeerHora(fila.Cell("J"))),
            CcPagadora = Truncar(LeerTexto(fila.Cell("K")), 100, "CAF-2026", f, "CC PAGADORA"),
            Pase = Truncar(LeerTexto(fila.Cell("L")), 100, "CAF-2026", f, "PASE"),
            Revisado = CombinarFechaHora(LeerFecha(fila.Cell("M"), "CAF-2026", f, "REVISADO FECHA"), LeerHora(fila.Cell("N"))),
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = DateTime.UtcNow,
        };
        if (aplicar) db.ExpedientesCaf.Add(caf);
        rep.Contar("ExpedientesCaf", "a insertar");
    }
    if (aplicar) await db.SaveChangesAsync();
}

// ============================================================================
// 4) Hoja Pagos (PAGOS PENDIENTES) → ExpedientesSeguro
// ============================================================================
if (rutaSeguros is not null)
{
    Console.WriteLine("Leyendo hoja Pagos (seguros)…");
    using var wb = new XLWorkbook(rutaSeguros);
    var ws = wb.Worksheet("Pagos");
    var ultima = ws.RangeUsed()!.LastRow().RowNumber();

    var yaExisten = (await db.ExpedientesSeguro.AsNoTracking()
        .Select(e => new { e.Expediente, e.Op }).ToListAsync())
        .Select(e => (e.Expediente, e.Op ?? "")).ToHashSet();

    for (int f = 2; f <= ultima; f++)
    {
        var fila = ws.Row(f);
        if (SinDato(fila, "A", "B", "C", "D")) continue;   // arrastre de fórmulas

        var expediente = ExpedienteKey.Normalizar(LeerTexto(fila.Cell("A")))
            ?? ExpedienteKey.Normalizar(LeerTexto(fila.Cell("B")));
        if (expediente is null)
        {
            rep.Rechazo("Seguros", f, $"expediente inválido ({LeerTexto(fila.Cell("A")) ?? "vacío"} / {LeerTexto(fila.Cell("B")) ?? "vacío"})");
            continue;
        }
        var op = LeerTexto(fila.Cell("C"));

        if (!yaExisten.Add((expediente, op ?? "")))
        {
            rep.Contar("ExpedientesSeguro", "ya existían o repetidos en planilla (omitidos)");
            continue;
        }

        var seguro = new ExpedienteSeguro
        {
            Expediente = expediente,
            Op = Truncar(op, 50, "Seguros", f, "OP"),
            Beneficiario = Truncar(LeerTexto(fila.Cell("D")), 255, "Seguros", f, "BENEFICIARIO"),
            ImporteNeto = LeerDecimal(fila.Cell("E"), "Seguros", f, "IMPORTE NETO"),
            Estado = Truncar(LeerTexto(fila.Cell("F")), 100, "Seguros", f, "ESTADO"),
            SeguroOpcionId = Mapear(seguroOpc, LeerTexto(fila.Cell("G")), "Seguros: SEGURO"),
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = DateTime.UtcNow,
        };
        if (aplicar) db.ExpedientesSeguro.Add(seguro);
        rep.Contar("ExpedientesSeguro", "a insertar");
    }
    if (aplicar) await db.SaveChangesAsync();
}

if (tx is not null)
{
    await tx.CommitAsync();
    Console.WriteLine("Transacción confirmada.");
}

// ---------------- Reporte ----------------
Console.WriteLine();
Console.WriteLine(rep.Render(aplicar, maxRechazos: 50));
if (rutaReporte is not null)
{
    File.WriteAllText(rutaReporte, rep.Render(aplicar, maxRechazos: int.MaxValue), Encoding.UTF8);
    Console.WriteLine($"Reporte completo guardado en: {rutaReporte}");
}
return 0;

// ============================================================================
// Helpers de lectura de celdas
// ============================================================================

// ¿Ninguna de las columnas indicadas tiene un dato real? (los centinelas "0"/"-" no cuentan)
static bool SinDato(IXLRow fila, params string[] columnas)
{
    foreach (var col in columnas)
    {
        var c = fila.Cell(col);
        switch (c.Value.Type)
        {
            case XLDataType.Blank:
            case XLDataType.Error:
                continue;
            case XLDataType.Text:
                var t = c.GetText().Trim();
                if (t.Length == 0 || t == "0" || t == "-") continue;
                return false;
            default:
                return false;   // número, fecha, hora o booleano: hay dato
        }
    }
    return true;
}

// Texto limpio; los centinelas de las planillas ("0", "-", vacío) se leen como null.
string? LeerTexto(IXLCell c)
{
    switch (c.Value.Type)
    {
        case XLDataType.Blank:
        case XLDataType.Error:
            return null;
        case XLDataType.Text:
            var t = c.GetText().Trim();
            return t.Length == 0 || t == "0" || t == "-" ? null : t;
        case XLDataType.Number:
            var n = c.GetDouble();
            if (n == 0) return null;
            return n == Math.Floor(n)
                ? ((long)n).ToString(CultureInfo.InvariantCulture)
                : n.ToString(CultureInfo.InvariantCulture);
        case XLDataType.Boolean:
            return c.GetBoolean() ? "TRUE" : null;
        case XLDataType.DateTime:
            return c.GetDateTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        case XLDataType.TimeSpan:
            return c.GetTimeSpan().ToString();
        default:
            return null;
    }
}

int? LeerEntero(IXLCell c)
{
    switch (c.Value.Type)
    {
        case XLDataType.Number:
            var n = c.GetDouble();
            return n == Math.Floor(n) && n != 0 ? (int)n : null;
        case XLDataType.Text:
            return int.TryParse(c.GetText().Trim(), out var v) && v != 0 ? v : null;
        default:
            return null;
    }
}

decimal? LeerDecimal(IXLCell c, string? hoja, int fila, string? campo)
{
    switch (c.Value.Type)
    {
        case XLDataType.Number:
            return Math.Round((decimal)c.GetDouble(), 2);   // limpia ruido binario del Excel
        case XLDataType.Text:
            var t = c.GetText().Trim();
            if (t.Length == 0 || t == "-") return null;
            if (decimal.TryParse(t, NumberStyles.Number, esAr, out var v)) return Math.Round(v, 2);
            if (hoja is not null) rep.Rechazo(hoja, fila, $"{campo}: importe ilegible \"{t}\" (queda vacío)");
            return null;
        default:
            return null;
    }
}

// Fechas: acepta DateTime real, serial de Excel y texto dd/MM/yyyy.
// Centinelas (0, "-", 31/12/1899 o cualquier fecha anterior a 1990) → null.
DateTime? LeerFecha(IXLCell c, string hoja, int fila, string campo)
{
    switch (c.Value.Type)
    {
        case XLDataType.Blank:
        case XLDataType.Error:
            return null;
        case XLDataType.DateTime:
            var d = c.GetDateTime();
            return d.Year < 1990 ? null : d;
        case XLDataType.Number:
            var n = c.GetDouble();
            if (n == 0) return null;
            if (n is > 32874 and < 80000) return DateTime.FromOADate(n);   // 32874 = 01/01/1990
            rep.Rechazo(hoja, fila, $"{campo}: número {n} no parece una fecha (queda vacía)");
            return null;
        case XLDataType.Text:
            var t = c.GetText().Trim();
            if (t.Length == 0 || t == "0" || t == "-") return null;
            if (DateTime.TryParse(t, esAr, DateTimeStyles.None, out var v))
                return v.Year < 1990 ? null : v;
            rep.Rechazo(hoja, fila, $"{campo}: fecha ilegible \"{t}\" (queda vacía)");
            return null;
        default:
            return null;
    }
}

// Col K del tablero: la misma columna mezcla fechas con marcas ("CCOO", "N/C", "PAV",
// "Anulado"). Devuelve la fecha tipada o el motivo textual, nunca los dos.
(DateTime?, string?) LeerFechaOMotivo(IXLCell c, string hoja, int fila, string campo)
{
    if (c.Value.Type == XLDataType.Text)
    {
        var t = c.GetText().Trim();
        if (t.Length == 0 || t == "0" || t == "-") return (null, null);
        if (DateTime.TryParse(t, esAr, DateTimeStyles.None, out var v))
            return (v.Year < 1990 ? null : v, null);
        return (null, t);
    }
    return (LeerFecha(c, hoja, fila, campo), null);
}

TimeSpan? LeerHora(IXLCell c) => c.Value.Type switch
{
    XLDataType.TimeSpan => c.GetTimeSpan(),
    XLDataType.Number => TimeSpan.FromDays(c.GetDouble() % 1),
    XLDataType.DateTime => c.GetDateTime().TimeOfDay,
    _ => null,
};

static DateTime? CombinarFechaHora(DateTime? fecha, TimeSpan? hora)
    => fecha is null ? null : fecha.Value.Date + (hora ?? TimeSpan.Zero);

bool? LeerCafSiNo(IXLCell c)
{
    var t = LeerTexto(c);
    if (t is null) return null;   // "-" = sin definir
    if (t.Equals("CAF", StringComparison.OrdinalIgnoreCase)
        || t.Equals("SI", StringComparison.OrdinalIgnoreCase)
        || t.Equals("SÍ", StringComparison.OrdinalIgnoreCase)) return true;
    if (t.Equals("NO CAF", StringComparison.OrdinalIgnoreCase)
        || t.Equals("NO", StringComparison.OrdinalIgnoreCase)) return false;
    rep.NoMapeado("PAGOS: CAF SI/NO", t);
    return null;
}

int? Mapear(Dictionary<string, int> lookup, string? valor, string campo)
{
    if (valor is null) return null;
    if (lookup.TryGetValue(valor.Trim(), out var id)) return id;
    rep.NoMapeado(campo, valor);
    return null;
}

string? Truncar(string? valor, int max, string hoja, int fila, string campo)
{
    if (valor is null || valor.Length <= max) return valor;
    rep.Rechazo(hoja, fila, $"{campo}: recortado a {max} caracteres (\"{valor[..Math.Min(40, valor.Length)]}…\")");
    return valor[..max];
}

// Bug de la fórmula del Excel CAF: expediente financiera con 9+ dígitos (ceros de más).
static string? NormalizarConCerosDeMas(string? exp)
{
    if (exp is null) return null;
    var m = System.Text.RegularExpressions.Regex.Match(exp.Trim(), @"^0*(\d{6,8})\s*[/\- ]\s*(\d{2})$");
    return m.Success ? $"{m.Groups[1].Value.PadLeft(8, '0')}/{m.Groups[2].Value}" : null;
}

static string BuscarRaizRepo()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SAF.csproj")))
        dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}

// ============================================================================
// Reporte
// ============================================================================
internal sealed class Reporte
{
    private readonly Dictionary<string, Dictionary<string, int>> _conteos = new();
    private readonly List<string> _rechazos = new();
    private readonly Dictionary<string, Dictionary<string, int>> _noMapeados = new();

    public void Contar(string tabla, string categoria)
    {
        if (!_conteos.TryGetValue(tabla, out var cat)) _conteos[tabla] = cat = new();
        cat[categoria] = cat.GetValueOrDefault(categoria) + 1;
    }

    public void Rechazo(string hoja, int fila, string motivo)
        => _rechazos.Add($"  [{hoja} fila {fila}] {motivo}");

    public void NoMapeado(string campo, string valor)
    {
        if (!_noMapeados.TryGetValue(campo, out var vals)) _noMapeados[campo] = vals = new(StringComparer.OrdinalIgnoreCase);
        vals[valor.Trim()] = vals.GetValueOrDefault(valor.Trim()) + 1;
    }

    public string Render(bool aplicado, int maxRechazos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("================ RESUMEN ================");
        sb.AppendLine(aplicado ? "(cambios APLICADOS a la base)" : "(dry-run: NO se escribió nada)");
        foreach (var (tabla, cats) in _conteos)
        {
            sb.AppendLine($"{tabla}:");
            foreach (var (cat, n) in cats)
                sb.AppendLine($"  {cat}: {n}");
        }

        if (_noMapeados.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== Valores sin correspondencia (quedan vacíos; agregar la opción y re-correr si corresponde) ===");
            foreach (var (campo, vals) in _noMapeados)
            {
                sb.AppendLine($"{campo}:");
                foreach (var (v, n) in vals.OrderByDescending(kv => kv.Value))
                    sb.AppendLine($"  \"{v}\" × {n}");
            }
        }

        if (_rechazos.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"=== Avisos y filas rechazadas ({_rechazos.Count}) ===");
            foreach (var r in _rechazos.Take(maxRechazos)) sb.AppendLine(r);
            if (_rechazos.Count > maxRechazos) sb.AppendLine($"  … y {_rechazos.Count - maxRechazos} más (ver reporte completo con --reporte)");
        }
        return sb.ToString();
    }
}
