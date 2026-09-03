#nullable enable
using SAF.Application.Common;

namespace SAF.Application.Pagos;

/// <summary>
/// Una fila del ledger cuyo expediente difiere del vigente en IVC. ExpedienteActual
/// es lo que el usuario vio en el diálogo: al aplicar, la fila solo se corrige si
/// todavía dice eso (guarda contra ediciones concurrentes).
/// </summary>
public sealed record CorreccionExpediente(
    int DevengadoId,
    string TipoDev,
    int NroDev,
    DateTime? FechaImputacion,
    decimal? ImportePp,
    string? ExpedienteActual,
    string ExpedienteNuevo);

/// <summary>
/// Resultado del diff de expedientes SAF ↔ IVC. Las claves ambiguas son devengados
/// con líneas idénticas y expedientes distintos en IVC: no hay forma de saber cuál
/// corresponde a cada fila, así que se reportan en vez de adivinar.
/// </summary>
public sealed record DeteccionCorrecciones(
    IReadOnlyList<CorreccionExpediente> Correcciones,
    IReadOnlyList<string> ClavesAmbiguas);

/// <summary>
/// Detecta expedientes corregidos en IVC que el ledger de SAF no vio: la corrección
/// del Excel se hace antes de la recarga diaria de DEVENGADOS, pero la sincronización
/// es append-only y su clave de idempotencia (tipo, nro, fecha, importe) no incluye
/// el expediente, así que una fila ya importada conserva el valor viejo para siempre.
/// El mismo cuarteto que garantiza la idempotencia sirve acá como identidad de fila
/// para traer el valor vigente.
/// </summary>
public static class CorreccionExpedienteDetector
{
    /// <summary>Fila de la tabla acumulativa de SAF.</summary>
    public sealed record FilaLedger(
        int Id, string TipoDev, int NroDev, DateTime? FechaImputacion, decimal? ImportePp, string? Expediente);

    /// <summary>Fila de IVC.dbo.DEVENGADOS (con el filtro de la vista Pagos ya aplicado).</summary>
    public sealed record FilaIvc(
        string TipoDev, int NroDev, DateTime? FechaImputacion, decimal? ImportePp, string? EeFinanciera);

    // Largo de la columna expediente del ledger: un valor de IVC que no entre haría
    // fallar el guardado del lote entero, así que ni se propone.
    private const int MaxLargoExpediente = 50;

    public static DeteccionCorrecciones Detectar(
        IReadOnlyList<FilaLedger> ledger, IReadOnlyList<FilaIvc> ivc)
    {
        // Expedientes vigentes por cuarteto. Se comparan NORMALIZADOS (misma regla que
        // la ingesta): así una diferencia de formato puro también se corrige — el valor
        // guardado es el que cruza con CAF/Seguros/SADE, y un crudo no cruza.
        var grupos = new Dictionary<(string, int, DateTime?, decimal?), List<string>>();
        foreach (var f in ivc)
        {
            var clave = (f.TipoDev, f.NroDev, f.FechaImputacion, f.ImportePp);
            if (!grupos.TryGetValue(clave, out var valores))
                grupos[clave] = valores = new List<string>();

            var exp = Normalizado(f.EeFinanciera);
            if (exp is not null && exp.Length <= MaxLargoExpediente
                && !valores.Contains(exp, StringComparer.OrdinalIgnoreCase))
                valores.Add(exp);
        }

        var correcciones = new List<CorreccionExpediente>();
        var ambiguas = new List<string>();
        var ambiguasVistas = new HashSet<(string, int)>();

        foreach (var f in ledger)
        {
            // Sin match (alta manual, fecha que IVC ya no retiene) o IVC sin expediente:
            // no hay valor vigente contra el cual corregir. Blanquear jamás se propone.
            if (!grupos.TryGetValue((f.TipoDev, f.NroDev, f.FechaImputacion, f.ImportePp), out var valores)
                || valores.Count == 0)
                continue;

            if (valores.Count == 1)
            {
                if (!string.Equals(f.Expediente, valores[0], StringComparison.OrdinalIgnoreCase))
                    correcciones.Add(new CorreccionExpediente(
                        f.Id, f.TipoDev, f.NroDev, f.FechaImputacion, f.ImportePp, f.Expediente, valores[0]));
                continue;
            }

            // Líneas idénticas con expedientes distintos en IVC: si el valor de SAF es
            // uno de ellos se asume correcto; si no, se reporta sin tocar (adivinar
            // cuál le toca a esta fila corrompería la clave de cruce en silencio).
            if (valores.Any(v => string.Equals(f.Expediente, v, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (ambiguasVistas.Add((f.TipoDev, f.NroDev)))
                ambiguas.Add($"{f.TipoDev} {f.NroDev}");
        }

        // Orden estable para el diálogo de confirmación (y para los tests).
        correcciones.Sort((a, b) =>
        {
            var porFecha = Nullable.Compare(a.FechaImputacion, b.FechaImputacion);
            if (porFecha != 0) return porFecha;
            var porTipo = string.Compare(a.TipoDev, b.TipoDev, StringComparison.Ordinal);
            return porTipo != 0 ? porTipo : a.NroDev.CompareTo(b.NroDev);
        });

        return new DeteccionCorrecciones(correcciones, ambiguas);
    }

    // Misma regla que la ingesta (sync y alta manual): clave financiera normalizada,
    // o el crudo con Trim si el formato no se reconoce.
    private static string? Normalizado(string? expediente)
    {
        if (string.IsNullOrWhiteSpace(expediente)) return null;
        return ExpedienteKey.Normalizar(expediente) ?? expediente.Trim();
    }
}
