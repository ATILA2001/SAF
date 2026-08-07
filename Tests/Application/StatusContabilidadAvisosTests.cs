using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.StatusContabilidad.Dtos;

namespace Tests.Application;

/// <summary>
/// Avisos de pedidos de factura atrasados: se marca el pedido que ya corresponde
/// hacer cuando pasaron más de 7 días corridos del anterior sin factura ingresada.
/// </summary>
[TestClass]
public class StatusContabilidadAvisosTests
{
    private static StatusContabilidadViewModel Fila() => new()
    {
        FechaPedidoFactura1 = DateTime.Today.AddDays(-8),
    };

    [TestMethod]
    public void Pedido2_MasDe7DiasDelPedido1_SinIngreso_SeMarca()
        => Assert.IsTrue(Fila().PedidoFactura2Atrasado);

    [TestMethod]
    public void Pedido2_Exactamente7Dias_NoSeMarca()
    {
        var vm = Fila();
        vm.FechaPedidoFactura1 = DateTime.Today.AddDays(-7);
        Assert.IsFalse(vm.PedidoFactura2Atrasado);
    }

    [TestMethod]
    public void Pedido2_ConFacturaIngresada_NoSeMarca()
    {
        var vm = Fila();
        vm.FechaIngresoFactura = DateTime.Today;
        Assert.IsFalse(vm.PedidoFactura2Atrasado);
    }

    [TestMethod]
    public void Pedido2_ConMarcaSinFactura_NoSeMarca()
    {
        var vm = Fila();
        vm.SinFacturaMotivo = "Anulado";
        Assert.IsFalse(vm.PedidoFactura2Atrasado);
    }

    [TestMethod]
    public void Pedido2_YaCargado_NoSeMarca()
    {
        var vm = Fila();
        vm.FechaPedidoFactura2 = DateTime.Today;
        Assert.IsFalse(vm.PedidoFactura2Atrasado);
    }

    [TestMethod]
    public void Pedido2_SinFecha1_NoSeMarca()
    {
        var vm = Fila();
        vm.FechaPedidoFactura1 = null;
        Assert.IsFalse(vm.PedidoFactura2Atrasado);
    }

    [TestMethod]
    public void Pedido3_MasDe7DiasDelPedido2_SinIngreso_SeMarca()
    {
        var vm = Fila();
        vm.FechaPedidoFactura2 = DateTime.Today.AddDays(-8);
        Assert.IsTrue(vm.PedidoFactura3Atrasado);
    }

    [TestMethod]
    public void Pedido3_SinPedido2_NoSeMarca()
        => Assert.IsFalse(Fila().PedidoFactura3Atrasado);

    [TestMethod]
    public void Pedido3_YaCargado_NoSeMarca()
    {
        var vm = Fila();
        vm.FechaPedidoFactura2 = DateTime.Today.AddDays(-8);
        vm.ReiterarPedidoFactura3 = DateTime.Today;
        Assert.IsFalse(vm.PedidoFactura3Atrasado);
    }

    [TestMethod]
    public void Pedido3_ConFacturaIngresada_NoSeMarca()
    {
        var vm = Fila();
        vm.FechaPedidoFactura2 = DateTime.Today.AddDays(-8);
        vm.FechaIngresoFactura = DateTime.Today;
        Assert.IsFalse(vm.PedidoFactura3Atrasado);
    }
}