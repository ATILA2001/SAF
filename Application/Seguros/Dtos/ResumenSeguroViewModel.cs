#nullable enable

namespace SAF.Application.Seguros.Dtos;

/// <summary>
/// Seguro y estado de un expediente en la tabla de seguros, para las columnas Seguros
/// Teso y Estado Seguros de Pagos. Un expediente puede tener varias OPs (~15% de los
/// que cruzan con Pagos): el seguro se resume con "peor caso gana", pero el estado es
/// texto libre y no tiene un peor caso — si las OPs difieren no se muestra ninguno y
/// el detalle por OP explica el porqué.
/// </summary>
public class ResumenSeguroViewModel
{
    /// <summary>Seguro con la regla "peor caso gana" (solo cuenta filas con seguro asignado).</summary>
    public string? Seguro { get; set; }

    /// <summary>
    /// Estado del expediente, solo si TODAS sus OPs coinciden (una OP sin estado ya
    /// rompe la coincidencia): mostrar el de una sola sería informar el estado de todas.
    /// </summary>
    public string? Estado { get; set; }

    public int Ops { get; set; }

    /// <summary>Las OPs del expediente, para ver el seguro y el estado de cada una.</summary>
    public IReadOnlyList<LineaSeguroViewModel> Lineas { get; set; } = Array.Empty<LineaSeguroViewModel>();
}
