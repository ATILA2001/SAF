#nullable enable
using SAF.Application.Common;
using SAF.Application.Seguros.Dtos;

namespace SAF.Application.Seguros;

/// <summary>
/// Reglas de una fila de seguro. Fuente única: la usa el servicio (integridad,
/// no se puede saltear) y la grilla (aviso antes de perder lo cargado).
/// </summary>
public static class SeguroValidator
{
    public static IReadOnlyList<string> Validar(SeguroViewModel vm)
    {
        var errores = new List<string>();

        Validaciones.Expediente(errores, vm.Expediente);

        // Sin estado de seguro la fila no participa del cruce "peor caso gana" de Pagos:
        // quedaría cargada pero invisible para la vista que la necesita.
        if (vm.SeguroOpcionId is null or <= 0)
            errores.Add("Seleccioná el estado del seguro (sin estado la fila no se cruza con Pagos).");

        Validaciones.ImporteNoNegativo(errores, vm.ImporteNeto, "El importe neto");

        Validaciones.Largo(errores, vm.Op, 50, "OP");
        Validaciones.Largo(errores, vm.Beneficiario, 255, "Beneficiario");
        Validaciones.Largo(errores, vm.Estado, 100, "Estado");

        return errores;
    }
}