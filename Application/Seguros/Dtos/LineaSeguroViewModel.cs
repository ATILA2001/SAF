#nullable enable

namespace SAF.Application.Seguros.Dtos;

/// <summary>Una OP del expediente en seguros, para mostrar el detrás del seguro y su estado.</summary>
public class LineaSeguroViewModel
{
    public string? Op { get; set; }
    public decimal? ImporteNeto { get; set; }
    public string? Seguro { get; set; }
    public string? Estado { get; set; }
}
