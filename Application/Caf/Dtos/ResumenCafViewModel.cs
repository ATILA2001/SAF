#nullable enable

namespace SAF.Application.Caf.Dtos;

/// <summary>
/// Estado de pago de un expediente en CAF. Un expediente tiene varias OPs (94% de los
/// casos en la planilla real) y más de la mitad de esos están pagados solo en parte:
/// por eso no alcanza con la fecha, hay que saber cuántas OPs faltan.
/// </summary>
public class ResumenCafViewModel
{
    /// <summary>Fecha de pago más reciente entre las OPs ya pagadas.</summary>
    public DateTime? UltimoPago { get; set; }

    public int Ops { get; set; }
    public int OpsPagadas { get; set; }

    /// <summary>Las OPs del expediente, las pendientes primero.</summary>
    public IReadOnlyList<LineaCafViewModel> Lineas { get; set; } = Array.Empty<LineaCafViewModel>();

    /// <summary>El expediente está saldado: recién ahí la fecha representa "pagado".</summary>
    public bool TodasPagadas => Ops > 0 && OpsPagadas == Ops;

    /// <summary>Se pagó algo pero falta: mostrar la fecha sería informar un pago completo.</summary>
    public bool Parcial => OpsPagadas > 0 && OpsPagadas < Ops;
}
