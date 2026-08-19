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
    /// Los pedidos de factura van en orden: 1 → 2 → reiteración (3). Cada fecha
    /// cargada se compara contra la última anterior que tenga valor (los huecos se
    /// saltean). El pedido 1 no se edita (viene del devengado) pero sí es el piso de
    /// los siguientes. Ni el rechazo ni el ingreso de la factura pueden ser anteriores
    /// a ningún pedido (ambos suceden después de pedirla).
    /// </summary>
    private static void ValidarCronologia(List<string> errores, StatusContabilidadViewModel vm)
    {
        var pedidos = new (DateTime? Fecha, string Nombre)[]
        {
            (vm.FechaPedidoFactura1, "Fecha pedido de factura 1"),
            (vm.FechaPedidoFactura2, "Fecha pedido de factura 2"),
            (vm.ReiterarPedidoFactura3, "Reiterar pedido de factura 3"),
        };

        (DateTime Fecha, string Nombre)? previa = null;
        foreach (var (fecha, nombre) in pedidos)
        {
            if (fecha is not DateTime actual) continue;

            if (previa is { } p && actual.Date < p.Fecha.Date)
                errores.Add($"{nombre} no puede ser anterior a {p.Nombre} ({p.Fecha:dd/MM/yyyy}).");

            previa = (actual, nombre);
        }

        // Contra el pedido más tardío alcanza: si además hay pedidos desordenados,
        // el error ya lo marcó la pasada de arriba.
        var tope = pedidos
            .Where(p => p.Fecha is not null)
            .OrderByDescending(p => p.Fecha)
            .FirstOrDefault();
        if (tope.Fecha is not DateTime pedidoMasTardio) return;

        ValidarPosteriorAlPedido(errores, vm.FechaRechazo, "Fecha de rechazo", pedidoMasTardio, tope.Nombre!);
        ValidarPosteriorAlPedido(errores, vm.FechaIngresoFactura, "Fecha de ingreso de factura", pedidoMasTardio, tope.Nombre!);
    }

    private static void ValidarPosteriorAlPedido(
        List<string> errores, DateTime? fecha, string nombre, DateTime pedido, string nombrePedido)
    {
        if (fecha is DateTime f && f.Date < pedido.Date)
            errores.Add($"{nombre} no puede ser anterior a {nombrePedido} ({pedido:dd/MM/yyyy}).");
    }
}