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

        // El resumen de Pagos exige que todas las OPs del expediente coincidan: una
        // fila sin seguro deja el resumen vacío para todo el expediente.
        if (vm.SeguroOpcionId is null or <= 0)
            errores.Add("Seleccioná el estado del seguro (sin él, el expediente queda sin resumen en Pagos).");

        Validaciones.ImporteNoNegativo(errores, vm.ImporteNeto, "El importe neto");

        Validaciones.Largo(errores, vm.Op, 50, "OP");
        Validaciones.Largo(errores, vm.Beneficiario, 255, "Beneficiario");
        Validaciones.Largo(errores, vm.Estado, 100, "Estado");

        return errores;
    }
}