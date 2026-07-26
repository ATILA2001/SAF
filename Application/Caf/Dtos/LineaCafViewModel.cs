#nullable enable

namespace SAF.Application.Caf.Dtos;

/// <summary>Una OP del expediente en CAF, para mostrar el detrás de la fecha de pago.</summary>
public class LineaCafViewModel
{
    public string? Op { get; set; }
    public decimal? ImporteNeto { get; set; }

    /// <summary>Sin fecha = OP pendiente de pago.</summary>
    public DateTime? FechaPago { get; set; }
}
