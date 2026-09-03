using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.Pagos;
using static SAF.Application.Pagos.CorreccionExpedienteDetector;

namespace Tests.Application;

/// <summary>
/// El diff de expedientes SAF ↔ IVC es lo único que trae al ledger una corrección
/// hecha en la fuente: la sync es append-only y su clave de idempotencia (tipo, nro,
/// fecha, importe) no incluye el expediente, así que una fila ya importada conserva
/// el valor viejo hasta que este diff lo detecta. Adivinar mal acá corrompe la clave
/// de cruce de CAF/Seguros/SADE en silencio: los casos dudosos se reportan, no se tocan.
/// </summary>
[TestClass]
public class CorreccionExpedienteDetectorTests
{
    private static readonly DateTime Fecha = new(2026, 3, 1);

    private static FilaLedger Saf(int id, string? expediente, string tipo = "PRD", int nro = 10,
        DateTime? fecha = null, decimal? importe = 1000m)
        => new(id, tipo, nro, fecha ?? Fecha, importe, expediente);

    private static FilaIvc Ivc(string? expediente, string tipo = "PRD", int nro = 10,
        DateTime? fecha = null, decimal? importe = 1000m)
        => new(tipo, nro, fecha ?? Fecha, importe, expediente);

    [TestMethod]
    public void DetectaElExpedienteCorregidoEnIvc()
    {
        var resultado = Detectar(
            [Saf(5, "01212221/26")],
            [Ivc("99999999/26")]);

        Assert.AreEqual(1, resultado.Correcciones.Count);
        var c = resultado.Correcciones[0];
        Assert.AreEqual(5, c.DevengadoId);
        Assert.AreEqual("01212221/26", c.ExpedienteActual);
        Assert.AreEqual("99999999/26", c.ExpedienteNuevo);
        Assert.AreEqual(0, resultado.ClavesAmbiguas.Count);
    }

    [TestMethod]
    public void FormatosEquivalentesNoProponenNada()
    {
        // El valor de IVC se normaliza con la misma regla que la ingesta: el formato
        // SADE y la clave financiera son el mismo expediente, no una corrección.
        var resultado = Detectar(
            [Saf(1, "01212221/26")],
            [Ivc("EX-2026-01212221-GCABA-IVC")]);

        Assert.AreEqual(0, resultado.Correcciones.Count);
    }

    [TestMethod]
    public void UnCrudoGuardadoEnSafSeProponeNormalizar()
    {
        // Fila histórica que guardó el crudo: el valor guardado es el que cruza con
        // CAF/Seguros/SADE, así que llevarlo a la clave financiera ES una corrección.
        var resultado = Detectar(
            [Saf(1, "EX-2026-01212221-GCABA-IVC")],
            [Ivc("EX-2026-01212221-GCABA-IVC")]);

        Assert.AreEqual(1, resultado.Correcciones.Count);
        Assert.AreEqual("01212221/26", resultado.Correcciones[0].ExpedienteNuevo);
    }

    [TestMethod]
    public void GrupoAmbiguoSeReportaSinTocarNada()
    {
        // Dos líneas idénticas en IVC con expedientes distintos: no hay forma de saber
        // cuál corresponde a la fila de SAF. Una sola mención por devengado.
        var resultado = Detectar(
            [Saf(1, "11111111/26"), Saf(2, "11111111/26")],
            [Ivc("22222222/26"), Ivc("33333333/26")]);

        Assert.AreEqual(0, resultado.Correcciones.Count);
        Assert.AreEqual(1, resultado.ClavesAmbiguas.Count);
        Assert.AreEqual("PRD 10", resultado.ClavesAmbiguas[0]);
    }

    [TestMethod]
    public void GrupoAmbiguoConElValorDeSafPresenteSeAsumeCorrecto()
    {
        var resultado = Detectar(
            [Saf(1, "22222222/26")],
            [Ivc("22222222/26"), Ivc("33333333/26")]);

        Assert.AreEqual(0, resultado.Correcciones.Count);
        Assert.AreEqual(0, resultado.ClavesAmbiguas.Count);
    }

    [TestMethod]
    public void FilaSinMatchEnIvcNoSeToca()
    {
        // Alta manual o fecha que IVC ya no retiene: no hay valor vigente para comparar.
        var resultado = Detectar(
            [Saf(1, "01212221/26", tipo: "DGG", nro: 99)],
            [Ivc("99999999/26")]);

        Assert.AreEqual(0, resultado.Correcciones.Count);
        Assert.AreEqual(0, resultado.ClavesAmbiguas.Count);
    }

    [TestMethod]
    public void IvcSinExpedienteNuncaProponeBlanquear()
    {
        var resultado = Detectar(
            [Saf(1, "01212221/26")],
            [Ivc(null), Ivc("   ")]);

        Assert.AreEqual(0, resultado.Correcciones.Count);
    }

    [TestMethod]
    public void ExpedienteVacioEnSafSeCompletaConElDeIvc()
    {
        var resultado = Detectar(
            [Saf(1, null)],
            [Ivc("01212221/26")]);

        Assert.AreEqual(1, resultado.Correcciones.Count);
        Assert.IsNull(resultado.Correcciones[0].ExpedienteActual);
        Assert.AreEqual("01212221/26", resultado.Correcciones[0].ExpedienteNuevo);
    }

    [TestMethod]
    public void CadaLineaDelDevengadoRecibeSuCorreccion()
    {
        // Neto y retención comparten devengado pero son claves distintas (importe):
        // cada una matchea contra su propia línea de IVC.
        var resultado = Detectar(
            [Saf(1, "11111111/26", importe: 900m), Saf(2, "11111111/26", importe: 100m)],
            [Ivc("22222222/26", importe: 900m), Ivc("22222222/26", importe: 100m)]);

        Assert.AreEqual(2, resultado.Correcciones.Count);
        Assert.IsTrue(resultado.Correcciones.All(c => c.ExpedienteNuevo == "22222222/26"));
    }
}
