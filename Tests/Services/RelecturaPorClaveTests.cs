using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Implementations;

namespace Tests.Services;

/// <summary>
/// La relectura de UNA fila tras guardar (el refresco en el lugar de la grilla) tiene
/// que consultar por clave: las tablas de datos manuales crecen una fila por cada fila
/// editada, así que si la relectura las escaneara enteras, cada guardado se iría
/// degradando con el uso sin que nadie conecte el síntoma con la causa.
/// </summary>
[TestClass]
public class RelecturaPorClaveTests
{
    [TestMethod]
    public async Task PagosGetByIdNoEscaneaLasTablasManuales()
    {
        var devengadoRepo = new Mock<IDevengadoRepository>();
        var extraRepo = new Mock<IDevengadoExtraRepository>();
        var statusContabRepo = new Mock<IStatusContabilidadExtraRepository>();
        var cafRepo = new Mock<ICafRepository>();
        var seguroRepo = new Mock<ISeguroRepository>();

        var devengado = new Devengado
        {
            Id = 5, TipoDev = "PRD", NroDev = 10,
            FechaImputacion = new DateTime(2026, 3, 1),
            Expediente = "01212221/26", Empresa = "EMPRESA X", ImportePp = 1000m,
        };
        devengadoRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devengado);

        extraRepo.Setup(r => r.GetByDevengadoIdsAsync(
                It.Is<IReadOnlyCollection<int>>(ids => ids.Single() == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DevengadoExtra { DevengadoId = 5, TipoDev = "PRD", NroDev = 10, Observaciones = "obs" }]);
        statusContabRepo.Setup(r => r.GetByClaveAsync("PRD", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusContabilidadExtra
            {
                TipoDev = "PRD", NroDev = 10, FechaPedidoFactura2 = new DateTime(2026, 3, 5),
            });
        cafRepo.Setup(r => r.GetResumenCafByExpedientesAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, SAF.Application.Caf.Dtos.ResumenCafViewModel>());
        seguroRepo.Setup(r => r.GetSeguroByExpedientesAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var service = new PagosService(
            devengadoRepo.Object, extraRepo.Object, Mock.Of<ISadeRepository>(),
            Mock.Of<ISigafOpRepository>(), statusContabRepo.Object, cafRepo.Object, seguroRepo.Object);

        var vm = await service.GetByIdAsync(5);

        // La fila vuelve mapeada con sus datos manuales (mismo mapeo que la carga completa).
        Assert.IsNotNull(vm);
        Assert.AreEqual("obs", vm.Observaciones);
        Assert.AreEqual(new DateTime(2026, 3, 5), vm.PedidoFactura2);

        // Y ninguna tabla manual se leyó entera.
        extraRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never,
            "la relectura de una fila escaneó los extras de Pagos completos");
        statusContabRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never,
            "la relectura de una fila escaneó el tablero de Status completo");
    }

    [TestMethod]
    public async Task StatusGetPorDevengadoNoEscaneaLasTablasManuales()
    {
        var devengadoRepo = new Mock<IDevengadoRepository>();
        var pagosRepo = new Mock<IDevengadoExtraRepository>();
        var contaRepo = new Mock<IStatusContabilidadExtraRepository>();

        // Dos líneas del mismo devengado: la representante es la de mayor importe.
        var lineas = new List<Devengado>
        {
            new() { Id = 7, TipoDev = "PRD", NroDev = 20, ImportePp = 900m, Expediente = "01212221/26" },
            new() { Id = 8, TipoDev = "PRD", NroDev = 20, ImportePp = 100m, Expediente = "01212221/26" },
        };
        devengadoRepo.Setup(r => r.GetByClaveAsync("PRD", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lineas);

        pagosRepo.Setup(r => r.GetByDevengadoIdsAsync(
                It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(7) && ids.Contains(8)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DevengadoExtra { DevengadoId = 7, TipoDev = "PRD", NroDev = 20, Ccoo = "ccoo-7" }]);
        contaRepo.Setup(r => r.GetByClaveAsync("PRD", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StatusContabilidadExtra?)null);

        var service = new StatusContabilidadService(
            devengadoRepo.Object, pagosRepo.Object, contaRepo.Object, Mock.Of<ISadeRepository>());

        var vm = await service.GetPorDevengadoAsync("PRD", 20);

        // La fila del tablero agrupa las líneas igual que la carga completa.
        Assert.IsNotNull(vm);
        Assert.AreEqual(1000m, vm.ImporteTotal);
        Assert.AreEqual(2, vm.CantidadLineas);
        Assert.AreEqual("ccoo-7", vm.Ccoo); // el extra de la línea representante (la de mayor importe)

        pagosRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never,
            "la relectura de una fila escaneó los extras de Pagos completos");
        contaRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never,
            "la relectura de una fila escaneó el tablero de Status completo");
    }
}
