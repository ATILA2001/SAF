#nullable enable

namespace SAF.Application.Seguros.Dtos;

/// <summary>
/// Seguro y estado de un expediente en la tabla de seguros, para la columna de seguros
/// de Pagos. Un expediente puede tener varias OPs (~15% de los que cruzan con Pagos):
/// ambos valores se resumen solo por unanimidad — si las OPs difieren no se muestra
/// ninguno y es el detalle por OP el que enseña todos los casos.
/// </summary>
public class ResumenSeguroViewModel
{
    /// <summary>
    /// Seguro del expediente, solo si TODAS sus OPs coinciden (una OP sin seguro ya
    /// rompe la coincidencia): mostrar el de una sola sería informar el de todas.
    /// </summary>
    public string? Seguro { get; set; }

    /// <summary>
    /// Hay seguros distintos entre las OPs: <see cref="Seguro"/> queda vacío sin estar
    /// vacío el dato, y la vista lo destaca para que el detalle no pase inadvertido.
    /// </summary>
    public bool SegurosMezclados { get; set; }

    /// <summary>Estado del expediente, con la misma regla de unanimidad que el seguro.</summary>
    public string? Estado { get; set; }

    public int Ops { get; set; }

    /// <summary>Las OPs del expediente, para ver el seguro y el estado de cada una.</summary>
    public IReadOnlyList<LineaSeguroViewModel> Lineas { get; set; } = Array.Empty<LineaSeguroViewModel>();
}
