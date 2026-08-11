using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.StatusContabilidad;
using SAF.Application.StatusContabilidad.Dtos;

namespace Tests.Application;

[TestClass]
public class StatusContabilidadValidatorTests
{
    [TestMethod]
    public void Fila_SinDatosManuales_EsValida()
        => Assert.AreEqual(0, StatusContabilidadValidator.Validar(new StatusContabilidadViewModel()).Count);

    [TestMethod]
    public void Largos_EnElLimite_SonValidos()
    {
        var vm = new StatusContabilidadViewModel
        {
            ObservacionesCuentasPagar = new string('x', 500),
            ObservacionesLiquidaciones = new string('x', 500),
            SinFacturaMotivo = new string('x', 20),
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Largos_Excedidos_UnErrorPorCampo()
    {
        var vm = new StatusContabilidadViewModel
        {
            ObservacionesCuentasPagar = new string('x', 501),
            ObservacionesLiquidaciones = new string('x', 501),
            SinFacturaMotivo = new string('x', 21),
        };
        Assert.AreEqual(3, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Cronologia_EnOrden_EsValida()
    {
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura2 = new DateTime(2026, 1, 12),
            ReiterarPedidoFactura3 = new DateTime(2026, 1, 20),
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Cronologia_MismoDia_EsValida()
    {
        var dia = new DateTime(2026, 1, 5);
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura2 = dia,
            ReiterarPedidoFactura3 = dia,
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Cronologia_Pedido3AnteriorAlPedido2_EsInvalida()
    {
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura2 = new DateTime(2026, 1, 12),
            ReiterarPedidoFactura3 = new DateTime(2026, 1, 5),
        };
        var errores = StatusContabilidadValidator.Validar(vm);
        Assert.AreEqual(1, errores.Count);
        StringAssert.Contains(errores[0], "Reiterar pedido de factura 3");
        StringAssert.Contains(errores[0], "Fecha pedido de factura 2");
    }

    [TestMethod]
    public void Cronologia_Pedido1_NoSeValida()
    {
        // El pedido 1 viene del devengado (solo lectura): aunque sea posterior a lo
        // cargado por el usuario, no debe bloquear el guardado.
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura1 = new DateTime(2026, 1, 12),
            FechaPedidoFactura2 = new DateTime(2026, 1, 5),
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Cronologia_RechazoEIngreso_QuedanFueraDeLaCadena()
    {
        // Solo pedido 2 → reiteración (3) se ordenan entre sí: rechazo e ingreso
        // pueden ser anteriores sin marcar error.
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura2 = new DateTime(2026, 1, 20),
            ReiterarPedidoFactura3 = new DateTime(2026, 1, 25),
            FechaRechazo = new DateTime(2026, 1, 2),
            FechaIngresoFactura = new DateTime(2026, 1, 3),
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }
}
