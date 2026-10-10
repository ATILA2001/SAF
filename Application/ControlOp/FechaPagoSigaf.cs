#nullable enable

namespace SAF.Application.ControlOp;

/// <summary>
/// Cálculo de la "Fecha Pago No CAF" (MAX FECHA_PAGO por expediente, el MAXIFS de la
/// planilla) aplicando las correcciones de OP de SAF: una OP corregida deja de contar
/// para el expediente que tiene en la bajada y cuenta solo para el corregido. La
/// corrección de SAF prevalece aunque IVC corrija después por su lado.
/// </summary>
public static class FechaPagoSigaf
{
    /// <summary>
    /// Una OP de la bajada (o un grupo de OPs sin corregir de un expediente, que para
    /// el máximo da lo mismo): NroDocResp y Op vienen solo cuando hay que cruzarlos.
    /// </summary>
    public sealed record Linea(long? NroDocResp, string? Op, string? EeFinanciera, DateTime? FechaPago);

    public static IReadOnlyDictionary<string, DateTime> PorExpediente(
        IReadOnlyList<Linea> lineas, IReadOnlyCollection<CorreccionOpClave> correcciones)
    {
        var corregidas = correcciones.ToDictionary(
            c => Clave(c.NroDocResp, c.Op), c => c.ExpedienteCorregido, StringComparer.OrdinalIgnoreCase);

        var resultado = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var l in lineas)
        {
            if (l.FechaPago is not DateTime fecha) continue;

            // El expediente de la bajada se lleva a la clave del ledger: la bajada trae a
            // veces el número sin ceros a la izquierda y "False" en vez de vacío, y el
            // consumidor cruza por la clave normalizada.
            var expediente = corregidas.Count > 0 && l.NroDocResp is long nro && l.Op is not null
                && corregidas.TryGetValue(Clave(nro, l.Op), out var corregido)
                    ? corregido
                    : ExpedienteBajada.Normalizar(l.EeFinanciera);
            if (string.IsNullOrWhiteSpace(expediente)) continue;

            if (!resultado.TryGetValue(expediente, out var actual) || fecha > actual)
                resultado[expediente] = fecha;
        }
        return resultado;
    }

    /// <summary>Clave de cruce OP ↔ corrección; la OP se compara sin distinguir mayúsculas.</summary>
    public static string Clave(long nroDocResp, string op) => $"{nroDocResp}|{op.Trim().ToUpperInvariant()}";
}
