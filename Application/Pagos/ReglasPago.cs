#nullable enable

namespace SAF.Application.Pagos;

/// <summary>
/// Fórmulas de la vista Pagos que también necesita Control OP (la lista de control es
/// "las filas de Pagos sin Fecha Pago Total"): una sola definición, así las dos vistas
/// no pueden discrepar sobre qué significa "pagado". El tablero contable usa la de
/// "anulado" para el importe.
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

    // ANULADO — la línea no se paga (Control OP la saca de la lista) y en el tablero
    // contable su importe no suma. Se mira el Status DGAyF y no el Status OP, que también
    // tiene "anulado": una OP anulada no anula el devengado (se emite otra OP).
    // Va por nombre, como "avanzar": si se renombra la opción en Listas, la regla deja
    // de aplicarse.
    public const string StatusAnulado = "anulado";

    public static bool EsAnulado(string? statusDgayf)
        => string.Equals(statusDgayf, StatusAnulado, StringComparison.OrdinalIgnoreCase);

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
