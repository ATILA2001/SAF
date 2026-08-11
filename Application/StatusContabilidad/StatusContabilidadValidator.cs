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

        ValidarCronologia(errores, vm);

        return errores;
    }

    /// <summary>
    /// Los pedidos de factura editables van en orden: la reiteración (3) no puede ser
    /// anterior al pedido 2. El pedido 1 queda afuera: viene del devengado (fecha de
    /// imputación, otra tabla, solo lectura) y no debe bloquear lo que carga el usuario.
    /// Rechazo e ingreso de factura tampoco se ordenan: no son parte de la secuencia.
    /// </summary>
    private static void ValidarCronologia(List<string> errores, StatusContabilidadViewModel vm)
    {
        if (vm.ReiterarPedidoFactura3 is DateTime r3
            && vm.FechaPedidoFactura2 is DateTime f2
            && r3.Date < f2.Date)
        {
            errores.Add($"Reiterar pedido de factura 3 no puede ser anterior a Fecha pedido de factura 2 ({f2:dd/MM/yyyy}).");
        }
    }
}