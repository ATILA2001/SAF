#nullable enable

namespace SAF.Application.Pagos;

/// <summary>
/// Fórmulas de la vista Pagos que también necesita Control OP (la lista de control es
/// "las filas de Pagos sin Fecha Pago Total"): una sola definición, así las dos vistas
/// no pueden discrepar sobre qué significa "pagado".
/// </summary>
public static class ReglasPago
{
    // El mismo status con el que nace toda fila (sync y alta): una sola constante.
    public const string StatusAvanzar = Common.ReglasDevengado.StatusDgayfInicial;
    public const string StatusAvanzarCaf = "avanzar CAF";

    public static bool EsAvanzar(string? statusDgayf)
        => string.Equals(statusDgayf, StatusAvanzar, StringComparison.OrdinalIgnoreCase);

    public static bool EsAvanzarCaf(string? statusDgayf)
        => string.Equals(statusDgayf, StatusAvanzarCaf, StringComparison.OrdinalIgnoreCase);

    // FECHA PAGO TOTAL — Excel: =IF(StatusDGAyF="avanzar", P, IF(Q="","", MAX(P,Q)))
    //   P = Fecha Pago No CAF (derivada de SIGAF_OP)   Q = Fecha Pago CAF (derivada de tabla CAF)
    public static DateTime? CalcularFechaPagoTotal(string? statusDgayf, DateTime? noCaf, DateTime? caf)
    {
        if (EsAvanzar(statusDgayf))
            return noCaf;
        if (caf is null)
            return null;
        return noCaf is DateTime n && n > caf.Value ? n : caf;
    }
}
