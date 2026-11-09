#nullable enable
using SAF.Application.Pagos;

namespace SAF.Application.ControlOp;

/// <summary>
/// Filtro de la hoja "control OP" del Excel original:
///   =FILTER(PAGOS!D:R; (R="")*(G&lt;&gt;"anulado")*(G&lt;&gt;"avanzar CAF")*(I&lt;&gt;"")*(I&lt;A1))
/// con R = Fecha Pago Total, G = Status DGAyF, I = Fecha Firma OP y A1 = fecha de corte.
/// Son los devengados que ya tienen la OP firmada hace más de unos días y todavía no
/// cruzan con ninguna OP pagada: candidatos a tener una OP con el expediente mal cargado.
/// </summary>
public static class ReglasControlOp
{
    /// <summary>Días entre la firma de la OP y hoy para considerar que ya debería estar paga.</summary>
    public const int DiasCorte = 3;

    /// <summary>
    /// Status cuyo pago no sale de SIGAF: anulado no se paga; avanzar CAF se paga por CAF.
    /// Se comparan sin distinguir mayúsculas, como el Excel.
    /// </summary>
    public const string StatusAnulado = ReglasPago.StatusAnulado;
    public static readonly string[] StatusExcluidos = [StatusAnulado, ReglasPago.StatusAvanzarCaf];

    /// <summary>La celda A1 de la hoja: hoy menos los días de corte.</summary>
    public static DateTime FechaCorte(DateTime hoy) => hoy.Date.AddDays(-DiasCorte);

    /// <summary>
    /// ¿La fila entra en la lista de control? Firma OP cargada y anterior a la fecha de
    /// corte, status no excluido y sin Fecha Pago Total.
    /// </summary>
    public static bool EstaPendiente(DateTime? fechaFirmaOp, string? statusDgayf, DateTime? fechaPagoTotal, DateTime fechaCorte)
    {
        if (fechaFirmaOp is not DateTime firma || firma >= fechaCorte) return false;
        if (fechaPagoTotal is not null) return false;
        return !StatusExcluidos.Contains(statusDgayf ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }
}
