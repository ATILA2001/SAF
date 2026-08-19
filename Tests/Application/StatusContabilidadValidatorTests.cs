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
            FechaPedidoFactura1 = new DateTime(2026, 1, 5),
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
            FechaPedidoFactura1 = dia,
            FechaPedidoFactura2 = dia,
            ReiterarPedidoFactura3 = dia,
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Cronologia_Pedido2AnteriorAlPedido1_EsInvalida()
    {
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura1 = new DateTime(2026, 1, 12),
            FechaPedidoFactura2 = new DateTime(2026, 1, 5),
        };
        var errores = StatusContabilidadValidator.Validar(vm);
        Assert.AreEqual(1, errores.Count);
        StringAssert.Contains(errores[0], "Fecha pedido de factura 2");
        StringAssert.Contains(errores[0], "Fecha pedido de factura 1");
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
    public void Cronologia_SalteaHuecos_ComparaContraLaUltimaCargada()
    {
        // Sin pedido 2: la reiteración (3) se compara directo contra el pedido 1.
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura1 = new DateTime(2026, 1, 12),
            ReiterarPedidoFactura3 = new DateTime(2026, 1, 5),
        };
        var errores = StatusContabilidadValidator.Validar(vm);
        Assert.AreEqual(1, errores.Count);
        StringAssert.Contains(errores[0], "Reiterar pedido de factura 3");
        StringAssert.Contains(errores[0], "Fecha pedido de factura 1");
    }

    [TestMethod]
    public void Cronologia_Rechazo_QuedaFueraDeLaCadena()
    {
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura2 = new DateTime(2026, 1, 20),
            ReiterarPedidoFactura3 = new DateTime(2026, 1, 25),
            FechaRechazo = new DateTime(2026, 1, 2),
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Cronologia_IngresoPosteriorATodosLosPedidos_EsValido()
    {
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura1 = new DateTime(2026, 1, 5),
            FechaPedidoFactura2 = new DateTime(2026, 1, 12),
            FechaIngresoFactura = new DateTime(2026, 1, 12),
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Cronologia_IngresoAnteriorAlUltimoPedido_EsInvalido()
    {
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura1 = new DateTime(2026, 1, 5),
            FechaPedidoFactura2 = new DateTime(2026, 1, 12),
            FechaIngresoFactura = new DateTime(2026, 1, 8),
        };
        var errores = StatusContabilidadValidator.Validar(vm);
        Assert.AreEqual(1, errores.Count);
        StringAssert.Contains(errores[0], "Fecha de ingreso de factura");
        StringAssert.Contains(errores[0], "Fecha pedido de factura 2");
    }

    [TestMethod]
    public void Cronologia_IngresoAnteriorAlPedido1_EsInvalido()
    {
        // Sin pedidos cargados por el usuario: el pedido 1 (del devengado) igual
        // es piso para el ingreso.
        var vm = new StatusContabilidadViewModel
        {
            FechaPedidoFactura1 = new DateTime(2026, 1, 12),
            FechaIngresoFactura = new DateTime(2026, 1, 5),
        };
        var errores = StatusContabilidadValidator.Validar(vm);
        Assert.AreEqual(1, errores.Count);
        StringAssert.Contains(errores[0], "Fecha pedido de factura 1");
    }
}
