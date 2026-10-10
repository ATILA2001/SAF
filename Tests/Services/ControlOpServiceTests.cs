using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;
using SAF.Services.Implementations;

namespace Tests.Services;

/// <summary>
/// Control OP cierra el circuito de la hoja del Excel: la lista tiene que salir con la
/// misma fórmula que la columna de Pagos (correcciones incluidas, o un devengado recién
/// corregido seguiría figurando pendiente), y una corrección solo puede crearse sobre
/// una OP que exista en SIGAF, una sola vez, y hacia un expediente distinto.
/// </summary>
[TestClass]
public class ControlOpServiceTests
{
    private static readonly DateTime Corte = new(2026, 9, 29);

    private sealed class Armado
    {
        public Mock<IDevengadoExtraRepository> Extras { get; } = new();
        public Mock<IDevengadoRepository> Devengados { get; } = new();
        public Mock<ISigafOpRepository> Sigaf { get; } = new();
        public Mock<ISigafOpCorreccionRepository> Correcciones { get; } = new();
        public ControlOpService Servicio { get; }

        public Armado()
        {
            Correcciones.Setup(r => r.GetClavesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CorreccionOpClave>());
            Devengados.Setup(r => r.ExistenExpedientesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            Sigaf.Setup(r => r.GetFechaPagoByExpedientesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase));

            Servicio = new ControlOpService(
                Extras.Object, Devengados.Object, Sigaf.Object, Correcciones.Object,
                NullLogger<ControlOpService>.Instance);
        }
    }

    private static ControlOpViewModel Fila(int id, string expediente, string? status, DateTime firma, decimal importe = 1000m) => new()
    {
        DevengadoId = id, TipoDev = "PRD", NroDev = id, Expediente = expediente, Empresa = "EMPRESA",
        Importe = importe, FechaFirmaOp = firma, StatusDgayf = status,
    };

    private static OpSigafResumen Op(string op, long nro, string? expediente, decimal importe = 1000m,
        string? expCorregidoIvc = null, string correcionIvc = "0") => new()
    {
        Op = op, NroDocResp = nro, EeFinanciera = expediente, Beneficiario = "EMPRESA SA", ImporteOp = importe,
        FechaPago = new DateTime(2026, 9, 25), ExpCorregidoIvc = expCorregidoIvc, CorrecionIvc = correcionIvc,
    };

    [TestMethod]
    public async Task LaListaExcluyeLoQueYaCruzaConUnaOpPagada()
    {
        var a = new Armado();
        var firma = new DateTime(2026, 9, 10);
        a.Extras.Setup(r => r.GetConFirmaOpAnteriorAsync(Corte, ReglasControlOp.StatusExcluidos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ControlOpViewModel>
            {
                Fila(1, "00000001/26", "avanzar", firma),      // pagado segun la bajada: no entra
                Fila(2, "00000002/26", "avanzar", firma),      // sin pago: entra
                Fila(3, "00000003/26", "no avanzar", firma),   // status sin fecha total: entra
            });

        IReadOnlyCollection<string>? pedidos = null;
        a.Sigaf.Setup(r => r.GetFechaPagoByExpedientesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, CancellationToken>((e, _) => pedidos = e)
            .ReturnsAsync(new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase)
            {
                ["00000001/26"] = new DateTime(2026, 9, 20),
            });

        var lista = await a.Servicio.GetListaControlAsync(Corte);

        CollectionAssert.AreEqual(new[] { 2, 3 }, lista.Select(x => x.DevengadoId).ToArray());
        Assert.AreEqual("no avanzar", lista[1].StatusDgayf);
        CollectionAssert.AreEquivalent(new[] { "00000001/26", "00000002/26", "00000003/26" }, pedidos!.ToArray(),
            "a la bajada se le piden solo los expedientes de las filas filtradas");
    }

    [TestMethod]
    public async Task LasCandidatasTraenLasSenalesDeSaf()
    {
        var a = new Armado();
        var criterios = new CriteriosBusquedaOp("EMPRESA", new DateTime(2026, 9, 1), null, 1000m);

        a.Sigaf.Setup(r => r.BuscarOpsAsync(criterios, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpSigafResumen>
            {
                Op("C41-1-2026", 1, "000001/26", 1000.40m),    // igual (al peso) y existe en devengados (sin ceros en la bajada)
                Op("C41-2-2026", 2, "00000002/26", 400m),      // parcial, su expediente no existe
                Op("C41-3-2026", 3, "False", 5000m),           // "False" de la bajada = sin expediente
                Op("C41-4-2026", 4, "00000004/26", 1000m, expCorregidoIvc: "00000004/26", correcionIvc: "1"),
                // "False" en EXP_CORREGIDO con CORRECION 0: el artefacto del Excel, no una corrección.
                Op("C41-5-2026", 5, "00000005/26", 1000m, expCorregidoIvc: "False", correcionIvc: "0"),
            });
        a.Devengados.Setup(r => r.ExistenExpedientesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "00000001/26", "00000004/26" });
        a.Correcciones.Setup(r => r.GetClavesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CorreccionOpClave> { new(4, "c41-4-2026", "00000009/26") });

        var candidatas = await a.Servicio.BuscarCandidatasAsync(criterios);

        Assert.AreEqual(CoincidenciaImporte.Igual, candidatas[0].Coincidencia);
        Assert.IsFalse(candidatas[0].ExpedienteSinDevengado);

        Assert.AreEqual(CoincidenciaImporte.Parcial, candidatas[1].Coincidencia);
        Assert.IsTrue(candidatas[1].ExpedienteSinDevengado);

        Assert.IsNull(candidatas[2].ExpedienteSigaf, "el 'False' de la bajada no es un expediente");
        Assert.AreEqual("00000001/26", candidatas[0].ExpedienteSigaf, "el expediente de la bajada se normaliza");
        Assert.IsTrue(candidatas[2].ExpedienteSinDevengado);
        Assert.AreEqual(CoincidenciaImporte.Mayor, candidatas[2].Coincidencia);

        Assert.AreEqual("00000009/26", candidatas[3].CorregidaEnSafA, "la corrección se cruza sin distinguir mayúsculas en la OP");
        Assert.IsTrue(candidatas[3].CorregidaEnIvc);

        Assert.IsFalse(candidatas[4].CorregidaEnIvc, "CORRECION 0 con 'False' no es una corrección de IVC");
    }

    [TestMethod]
    public async Task EditarHaciaElExpedienteDeLaBajadaSeRechaza()
    {
        var a = new Armado();
        a.Correcciones.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SigafOpCorreccion { Id = 7, NroDocResp = 2, Op = "C41-2-2026", ExpedienteOriginal = "00000002/26", ExpedienteCorregido = "00000008/26" });

        var ex = await Assert.ThrowsExceptionAsync<ArgumentException>(() => a.Servicio.UpdateAsync(
            new CorreccionOpViewModel { Id = 7, NroDocResp = 2, Op = "C41-2-2026", ExpedienteCorregido = "000002/26" }));
        StringAssert.Contains(ex.Message, "eliminá la corrección");
        a.Correcciones.Verify(r => r.UpdateAsync(It.IsAny<SigafOpCorreccion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task BuscarSinCriteriosQueAcotenSeRechaza()
    {
        var a = new Armado();
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            a.Servicio.BuscarCandidatasAsync(new CriteriosBusquedaOp(null, null, null, 1000m)));
        a.Sigaf.Verify(r => r.BuscarOpsAsync(It.IsAny<CriteriosBusquedaOp>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CrearTomaLaFotoDeSigafYNormalizaElExpediente()
    {
        var a = new Armado();
        a.Sigaf.Setup(r => r.GetOpAsync(1, "C41-1-2026", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Op("C41-1-2026", 1, "00000001/26", 1000m));
        a.Correcciones.Setup(r => r.GetByClaveAsync(1, "C41-1-2026", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SigafOpCorreccion?)null);

        SigafOpCorreccion? guardada = null;
        a.Correcciones.Setup(r => r.AddAsync(It.IsAny<SigafOpCorreccion>(), It.IsAny<CancellationToken>()))
            .Callback<SigafOpCorreccion, CancellationToken>((e, _) => guardada = e)
            .Returns(Task.CompletedTask);

        var creada = await a.Servicio.CreateAsync(new CorreccionOpViewModel
        {
            NroDocResp = 1, Op = " C41-1-2026 ", ExpedienteCorregido = "000009/26",
        });

        Assert.IsNotNull(guardada);
        Assert.AreEqual("C41-1-2026", guardada.Op);
        Assert.AreEqual("00000001/26", guardada.ExpedienteOriginal);
        Assert.AreEqual("00000009/26", guardada.ExpedienteCorregido);
        Assert.AreEqual("EMPRESA SA", guardada.Beneficiario);
        Assert.AreEqual(1000m, guardada.ImporteOp);
        Assert.AreEqual(new DateTime(2026, 9, 25), guardada.FechaPago);
        Assert.AreEqual("00000009/26", creada.ExpedienteCorregido);
    }

    [TestMethod]
    public async Task CrearRechazaOpInexistenteDuplicadaOSinCambio()
    {
        var a = new Armado();
        a.Sigaf.Setup(r => r.GetOpAsync(1, "C41-1-2026", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Op("C41-1-2026", 1, "00000001/26"));
        a.Sigaf.Setup(r => r.GetOpAsync(2, "C41-2-2026", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Op("C41-2-2026", 2, "00000002/26"));
        a.Correcciones.Setup(r => r.GetByClaveAsync(2, "C41-2-2026", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SigafOpCorreccion { Id = 7, NroDocResp = 2, Op = "C41-2-2026", ExpedienteCorregido = "00000008/26" });

        // No existe en SIGAF con ese NRO_DOC_RESP.
        var inexistente = await Assert.ThrowsExceptionAsync<ArgumentException>(() => a.Servicio.CreateAsync(
            new CorreccionOpViewModel { NroDocResp = 99, Op = "C41-1-2026", ExpedienteCorregido = "000009/26" }));
        StringAssert.Contains(inexistente.Message, "no existe en la bajada de OP");

        // Ya tiene corrección.
        var duplicada = await Assert.ThrowsExceptionAsync<ArgumentException>(() => a.Servicio.CreateAsync(
            new CorreccionOpViewModel { NroDocResp = 2, Op = "C41-2-2026", ExpedienteCorregido = "000009/26" }));
        StringAssert.Contains(duplicada.Message, "00000008/26");

        // El expediente "corregido" es el que ya tiene.
        var sinCambio = await Assert.ThrowsExceptionAsync<ArgumentException>(() => a.Servicio.CreateAsync(
            new CorreccionOpViewModel { NroDocResp = 1, Op = "C41-1-2026", ExpedienteCorregido = "000001/26" }));
        StringAssert.Contains(sinCambio.Message, "no hace falta");

        a.Correcciones.Verify(r => r.AddAsync(It.IsAny<SigafOpCorreccion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CrearComparaConLaBajadaPorClaveNormalizada()
    {
        // En la bajada el expediente viene sin ceros a la izquierda. Corregir "hacia" el
        // mismo expediente (ya normalizado) no es una corrección: el cruce de Pagos ya lo
        // encuentra, y el alta tiene que decir lo mismo que la edición.
        var a = new Armado();
        a.Sigaf.Setup(r => r.GetOpAsync(1, "C41-1-2026", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Op("C41-1-2026", 1, "1212221/26"));
        a.Correcciones.Setup(r => r.GetByClaveAsync(1, "C41-1-2026", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SigafOpCorreccion?)null);

        var sinCambio = await Assert.ThrowsExceptionAsync<ArgumentException>(() => a.Servicio.CreateAsync(
            new CorreccionOpViewModel { NroDocResp = 1, Op = "C41-1-2026", ExpedienteCorregido = "01212221/26" }));
        StringAssert.Contains(sinCambio.Message, "no hace falta");
        a.Correcciones.Verify(r => r.AddAsync(It.IsAny<SigafOpCorreccion>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
