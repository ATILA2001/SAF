using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Application.StatusContabilidad.Dtos;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Implementations;

namespace Tests.Services;

/// <summary>
/// Un devengado anulado en Pagos (Status DGAyF "anulado") se ve con importe cero en el
/// tablero. El status es por línea y el tablero agrupa: cada línea anulada suma cero,
/// la principal es la de mayor importe no anulada, y solo el status DGAyF de Pagos
/// cuenta. Cada caso
/// se verifica por la carga completa y por la relectura de la fila tras guardar, que
/// tienen que dar lo mismo.
/// </summary>
[TestClass]
public class StatusContabilidadAnuladosTests
{
    // Pedido 1 viejo: sin factura, la fila marcaría el atraso del pedido 2.
    private static readonly DateTime Pedido1 = DateTime.Today.AddDays(-30);

    private static Devengado Linea(int id, decimal importe, string expediente = "01212221/26") => new()
    {
        Id = id, TipoDev = "PRD", NroDev = 20, FechaImputacion = Pedido1,
        Expediente = expediente, Empresa = "EMPRESA X", ImportePp = importe,
    };

    private static DevengadoExtra Pago(
        int devengadoId, string? statusDgayf, string? statusOp = null, string? ccoo = null) => new()
    {
        DevengadoId = devengadoId, TipoDev = "PRD", NroDev = 20, Ccoo = ccoo,
        StatusDgayfOpcion = statusDgayf is null ? null : new StatusDgayfOpcion { Nombre = statusDgayf },
        StatusOpOpcion = statusOp is null ? null : new StatusOpOpcion { Nombre = statusOp },
    };

    private static async Task<StatusContabilidadViewModel[]> CargarAsync(
        List<Devengado> lineas, List<DevengadoExtra> pagos, StatusContabilidadExtra? tablero = null)
    {
        var devengadoRepo = new Mock<IDevengadoRepository>();
        var pagosRepo = new Mock<IDevengadoExtraRepository>();
        var contaRepo = new Mock<IStatusContabilidadExtraRepository>();

        devengadoRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(lineas);
        devengadoRepo.Setup(r => r.GetByClaveAsync("PRD", 20, It.IsAny<CancellationToken>())).ReturnsAsync(lineas);
        pagosRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(pagos);
        pagosRepo.Setup(r => r.GetByDevengadoIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagos);
        var tableros = new List<StatusContabilidadExtra>();
        if (tablero is not null) tableros.Add(tablero);
        contaRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tableros);
        contaRepo.Setup(r => r.GetByClaveAsync("PRD", 20, It.IsAny<CancellationToken>())).ReturnsAsync(tablero);

        var service = new StatusContabilidadService(
            devengadoRepo.Object, pagosRepo.Object, contaRepo.Object, Mock.Of<ISadeRepository>());

        var completa = (await service.GetAllAsync()).Single();
        var relectura = await service.GetPorDevengadoAsync("PRD", 20);
        Assert.IsNotNull(relectura);
        return [completa, relectura];
    }

    [TestMethod]
    public async Task TodasLasLineasAnuladas_ImporteCeroYLaPrincipalEsLaDeMayorImporte()
    {
        var filas = await CargarAsync(
            [Linea(8, 100m, "09999999/26"), Linea(7, 900m)],
            [Pago(7, "anulado", ccoo: "ccoo-neto"), Pago(8, "anulado")]);

        foreach (var vm in filas)
        {
            Assert.AreEqual(0m, vm.ImporteTotal);
            Assert.IsTrue(vm.AnuladoEnPagos);
            Assert.AreEqual(2, vm.CantidadLineas);
            // Sin ninguna vigente, la fila sale de la de mayor importe (el neto).
            Assert.AreEqual("01212221/26", vm.Expediente);
            Assert.AreEqual("ccoo-neto", vm.Ccoo);
            Assert.AreEqual("anulado", vm.StatusDgayfNombre);
            Assert.IsNull(vm.Atraso, "un devengado anulado ya no espera factura");
        }
    }

    [TestMethod]
    public async Task NetoAnulado_LaPrincipalEsLaSegundaDeMayorImporte()
    {
        // Neto anulado, una retención vigente y otra sin datos cargados en Pagos.
        var filas = await CargarAsync(
            [Linea(7, 900m), Linea(8, 100m, "09999999/26"), Linea(9, 50m, "08888888/26")],
            [Pago(7, "anulado", ccoo: "ccoo-neto"), Pago(8, "avanzar", statusOp: "firmada", ccoo: "ccoo-ret")]);

        foreach (var vm in filas)
        {
            Assert.AreEqual(150m, vm.ImporteTotal);
            Assert.IsFalse(vm.AnuladoEnPagos);
            // Los datos de la fila salen de la retención de 100, no del neto anulado.
            Assert.AreEqual("09999999/26", vm.Expediente);
            Assert.AreEqual("avanzar", vm.StatusDgayfNombre);
            Assert.AreEqual("firmada", vm.FirmadaPorMiguel);
            Assert.AreEqual("ccoo-ret", vm.Ccoo);
            Assert.AreEqual("Pedido 2", vm.Atraso, "lo que sigue vigente todavía espera factura");

            // Detalle de líneas: la principal primero y el neto anulado al final.
            var colImporte = vm.DetalleLineas.Columnas.ToList().IndexOf("Importe");
            var importes = vm.DetalleLineas.Filas.Select(f => f.Valores[colImporte]).ToList();
            CollectionAssert.AreEqual(
                new[] { 100m, 50m, 900m }.Select(i => i.ToString("N2")).ToList(), importes);
            Assert.IsTrue(vm.DetalleLineas.Filas[0].EsPrincipal);
            Assert.IsFalse(vm.DetalleLineas.Filas.Skip(1).Any(f => f.EsPrincipal));
        }
    }

    [TestMethod]
    public async Task LasDosMayoresAnuladas_LaPrincipalEsLaTercera()
    {
        var filas = await CargarAsync(
            [Linea(7, 900m), Linea(8, 100m), Linea(9, 50m, "08888888/26")],
            [Pago(7, "anulado"), Pago(8, "anulado"), Pago(9, "avanzar")]);

        foreach (var vm in filas)
        {
            Assert.AreEqual(50m, vm.ImporteTotal);
            Assert.AreEqual("08888888/26", vm.Expediente);
            Assert.AreEqual("avanzar", vm.StatusDgayfNombre);
        }
    }

    [TestMethod]
    public async Task SinAnuladas_LaPrincipalSigueSiendoLaDeMayorImporte()
    {
        var filas = await CargarAsync(
            [Linea(8, 100m, "09999999/26"), Linea(7, 900m)],
            [Pago(7, "avanzar"), Pago(8, "avanzar")]);

        foreach (var vm in filas)
        {
            Assert.AreEqual(1000m, vm.ImporteTotal);
            Assert.AreEqual("01212221/26", vm.Expediente);
        }
    }

    [TestMethod]
    public async Task ElStatusSeComparaSinDistinguirMayusculas()
    {
        var filas = await CargarAsync([Linea(7, 900m)], [Pago(7, "Anulado")]);

        foreach (var vm in filas)
        {
            Assert.AreEqual(0m, vm.ImporteTotal);
            Assert.IsTrue(vm.AnuladoEnPagos);
        }
    }

    [TestMethod]
    public async Task ElStatusOpAnuladoNoAnulaElImporte()
    {
        var filas = await CargarAsync([Linea(7, 900m)], [Pago(7, "avanzar", statusOp: "anulado")]);

        foreach (var vm in filas)
        {
            Assert.AreEqual(900m, vm.ImporteTotal);
            Assert.IsFalse(vm.AnuladoEnPagos);
        }
    }

    [TestMethod]
    public async Task LasMarcasAnuladoDelTableroNoTocanElImporte()
    {
        var tablero = new StatusContabilidadExtra
        {
            TipoDev = "PRD", NroDev = 20,
            StatusContableOpcion = new StatusContableOpcion { Nombre = "Anulado" },
            SinFacturaMotivo = "Anulado",
        };

        var filas = await CargarAsync([Linea(7, 900m)], [Pago(7, "avanzar")], tablero);

        foreach (var vm in filas)
        {
            Assert.AreEqual(900m, vm.ImporteTotal);
            Assert.IsFalse(vm.AnuladoEnPagos);
        }
    }
}
