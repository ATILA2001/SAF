#nullable enable
using SAF.Application.Caf.Dtos;
using SAF.Application.Common;

namespace SAF.Application.Caf;

/// <summary>
/// Reglas de una fila de expediente CAF. Fuente única: la usa el servicio (integridad,
/// no se puede saltear) y la grilla (aviso antes de perder lo cargado).
/// </summary>
public static class CafValidator
{
    public static IReadOnlyList<string> Validar(CafViewModel vm)
    {
        var errores = new List<string>();

        Validaciones.Expediente(errores, vm.Expediente);

        if (vm.Anio is < 2000 or > 2100)
            errores.Add("El año es obligatorio y debe estar entre 2000 y 2100.");

        Validaciones.ImporteNoNegativo(errores, vm.ImporteNeto, "El importe neto");
        Validaciones.ImporteNoNegativo(errores, vm.Iibb, "IIBB");

        Validaciones.Largo(errores, vm.Op, 50, "OP");
        Validaciones.Largo(errores, vm.Beneficiario, 255, "Beneficiario");
        Validaciones.Largo(errores, vm.Cuenta, 50, "Cuenta");
        Validaciones.Largo(errores, vm.CcPagadora, 100, "CC Pagadora");
        Validaciones.Largo(errores, vm.Pase, 100, "Pase");

        return errores;
    }
}