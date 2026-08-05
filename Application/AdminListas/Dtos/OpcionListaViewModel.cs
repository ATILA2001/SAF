#nullable enable
using SAF.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace SAF.Application.AdminListas.Dtos;

/// <summary>
/// ViewModel de una fila de la vista Administración de listas. Sirve para las 6
/// tablas de opciones: todas comparten Nombre/Orden/Activo y solo la lista de
/// Seguros agrega EsOk (null en el resto, así no se exporta ni audita de más).
/// </summary>
public class OpcionListaViewModel
{
    [ExportIgnore, AuditIgnore]
    public int Id { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Orden")]
    public int Orden { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    /// <summary>Solo lista de Seguros: si la opción cuenta como "seguro en regla".</summary>
    [Display(Name = "Cuenta como OK")]
    public bool? EsOk { get; set; }
}
