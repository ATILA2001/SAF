#nullable enable
using SAF.Application.Common;
using SAF.Application.StatusContabilidad.Dtos;

namespace SAF.Application.StatusContabilidad;

/// <summary>
/// Reglas de una fila del tablero contable. Fuente única: la usa el servicio (integridad)
/// y la grilla (aviso antes de perder lo cargado).
/// Las filas nacen del ledger de devengados: acá solo se editan los campos propios.
/// </summary>
public static class StatusContabilidadValidator
{
    public static IReadOnlyList<string> Validar(StatusContabilidadViewModel vm)
    {
        var errores = new List<string>();

        Validaciones.Largo(errores, vm.ObservacionesCuentasPagar, 500, "Observaciones de Cuentas a Pagar");
        Validaciones.Largo(errores, vm.ObservacionesLiquidaciones, 500, "Observaciones de Liquidaciones");
        Validaciones.Largo(errores, vm.SinFacturaMotivo, 20, "El motivo sin factura");

        return errores;
    }
}