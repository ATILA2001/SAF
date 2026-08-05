#nullable enable
using SAF.Application.AdminListas.Dtos;
using SAF.Application.Common;

namespace SAF.Application.AdminListas;

/// <summary>
/// Reglas de una fila de opción de lista. Fuente única: la usa el servicio
/// (integridad, no se puede saltear) y la grilla (aviso antes de perder lo cargado).
/// </summary>
public static class OpcionListaValidator
{
    /// <summary>
    /// Tope del nombre: la clave de negocio de auditoría es "clave-lista:nombre"
    /// y la columna clave_negocio admite 200 caracteres.
    /// </summary>
    public const int NombreMaxLength = 150;

    public static IReadOnlyList<string> Validar(OpcionListaViewModel vm)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(vm.Nombre))
            errores.Add("El nombre es obligatorio.");
        else
            Validaciones.Largo(errores, vm.Nombre, NombreMaxLength, "El nombre");

        if (vm.Orden < 0)
            errores.Add("El orden no puede ser negativo.");

        return errores;
    }
}
