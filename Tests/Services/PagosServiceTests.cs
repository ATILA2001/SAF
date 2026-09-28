using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Application.Pagos.Dtos;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Implementations;

namespace Tests.Services;

/// <summary>
/// Desde que cada fila del ledger nace con su registro de datos manuales (la sync y
/// el alta lo crean), la tabla de extras de Pagos tiene el tamaño del ledger: la carga
/// por lote tiene que pedir solo los extras del lote, y el alta manual tiene que
/// persistir lo que el usuario cargó en la ventana (status, firma, CCOO, observaciones),
/// que antes se mostraba como opcional y se descartaba en silencio.
/// </summary>
[TestClass]
public class PagosServiceTests
{
    private static (Mock<IDevengadoRepository> Devengados, Mock<IDevengadoExtraRepository> Extras,
        Mock<IStatusContabilidadExtraRepository> StatusContab, PagosService Servicio) Armar()
    {
        var devengadoRepo = new Mock<IDevengadoRepository>();
        var extraRepo = new Mock<IDevengadoExtraRepository>();
        var statusContabRepo = new Mock<IStatusContabilidadExtraRepository>();
        var cafRepo = new Mock<ICafRepository>();
        var seguroRepo = new Mock<ISeguroRepository>();

        statusContabRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusContabilidadExtra>());
        cafRepo.Setup(r => r.GetResumenCafByExpedientesAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, SAF.Application.Caf.Dtos.ResumenCafViewModel>());
        seguroRepo.Setup(r => r.GetResumenSeguroByExpedientesAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, SAF.Application.Seguros.Dtos.ResumenSeguroViewModel>());

        var servicio = new PagosService(
            devengadoRepo.Object, extraRepo.Object, Mock.Of<ISadeRepository>(),
            Mock.Of<ISigafOpRepository>(), statusContabRepo.Object, cafRepo.Object, seguroRepo.Object);

        return (devengadoRepo, extraRepo, statusContabRepo, servicio);
    }

    [TestMethod]
    public async Task LaCargaPorLotePideSoloLosExtrasDelLote()
    {
        var (devengadoRepo, extraRepo, _, servicio) = Armar();

        devengadoRepo.Setup(r => r.GetPageAsync(100, 500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Devengado>
            {
                new() { Id = 5, TipoDev = "PRD", NroDev = 10, Expediente = "01212221/26", ImportePp = 1000m },
                new() { Id = 6, TipoDev = "PRD", NroDev = 11, Expediente = "01212222/26", ImportePp = 200m },
            });
        extraRepo.Setup(r => r.GetByDevengadoIdsAsync(
                It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 2 && ids.Contains(5) && ids.Contains(6)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DevengadoExtra
            {
                DevengadoId = 5, TipoDev = "PRD", NroDev = 10,
                StatusDgayfOpcion = new StatusDgayfOpcion { Id = 1, Nombre = "avanzar" }, StatusDgayfOpcionId = 1,
            }]);

        var lote = await servicio.GetPageAsync(100, 500);

        Assert.AreEqual(2, lote.Count);
        Assert.AreEqual("avanzar", lote.Single(p => p.Id == 5).StatusDgayfNombre);
        Assert.IsNull(lote.Single(p => p.Id == 6).StatusDgayfNombre);

        extraRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never,
            "la carga por lote releyó los extras de Pagos completos");
    }

    [TestMethod]
    public async Task ElAltaManualGuardaLosDatosCargadosEnLaMismaFila()
    {
        var (devengadoRepo, _, _, servicio) = Armar();

        DevengadoExtra? guardado = null;
        devengadoRepo.Setup(r => r.AddAsync(It.IsAny<Devengado>(), It.IsAny<DevengadoExtra>(), It.IsAny<CancellationToken>()))
            .Callback<Devengado, DevengadoExtra, CancellationToken>((_, extra, _) => guardado = extra)
            .Returns(Task.CompletedTask);

        var vm = new PagoViewModel
        {
            TipoDev = "prd", NroDev = 77, FechaDevengado = new DateTime(2026, 9, 25),
            Expediente = "01212221/26", Empresa = "EMPRESA X", Importe = 1000m,
            StatusDgayfOpcionId = 1,        // "avanzar", preseleccionado por la vista
            Observaciones = "cargado en el alta",
        };

        await servicio.CreateDevengadoAsync(vm);

        Assert.IsNotNull(guardado, "el alta tiene que persistir el extra junto con la fila");
        Assert.AreEqual(1, guardado.StatusDgayfOpcionId);
        Assert.AreEqual("cargado en el alta", guardado.Observaciones);
        Assert.AreEqual("PRD", guardado.TipoDev, "misma normalización que la fila del ledger");
        Assert.AreEqual(77, guardado.NroDev);
    }
}
