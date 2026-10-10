using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.ControlOp;
using static SAF.Application.ControlOp.FechaPagoSigaf;

namespace Tests.Application;

/// <summary>
/// "Fecha Pago No CAF" es el MAX(FECHA_PAGO) por expediente del SIGAF, pero con las
/// correcciones de OP aplicadas: una OP corregida tiene que dejar de contar para el
/// expediente que tiene en SIGAF (si no, el expediente equivocado sigue figurando
/// pagado) y contar para el corregido (si no, la corrección no sirve de nada).
/// </summary>
[TestClass]
public class FechaPagoSigafTests
{
    private static readonly DateTime F1 = new(2026, 9, 1);
    private static readonly DateTime F2 = new(2026, 9, 15);

    [TestMethod]
    public void SinCorreccionesEsElMaximoPorExpediente()
    {
        var resultado = PorExpediente(
        [
            new Linea(1, "C41-1-2026", "00000001/26", F1),
            new Linea(2, "C41-2-2026", "00000001/26", F2),
            new Linea(3, "C41-3-2026", "00000002/26", F1),
        ], []);

        Assert.AreEqual(F2, resultado["00000001/26"]);
        Assert.AreEqual(F1, resultado["00000002/26"]);
    }

    [TestMethod]
    public void LaOpCorregidaSeMueveAlExpedienteCorregido()
    {
        var resultado = PorExpediente(
        [
            new Linea(1, "C41-1-2026", "00000001/26", F2),   // en SIGAF figura en el 1, pero es del 9
            new Linea(3, "C41-3-2026", "00000002/26", F1),
        ],
        [new CorreccionOpClave(1, "C41-1-2026", "00000009/26")]);

        Assert.AreEqual(F2, resultado["00000009/26"], "el expediente corregido tiene que recibir la OP");
        Assert.IsFalse(resultado.ContainsKey("00000001/26"), "el expediente equivocado no puede seguir figurando pagado");
        Assert.AreEqual(F1, resultado["00000002/26"]);
    }

    [TestMethod]
    public void LaOpSeCruzaPorNroDocRespYOpSinDistinguirMayusculas()
    {
        var resultado = PorExpediente(
        [
            new Linea(1, "c41-1-2026", "00000001/26", F1),
            // Misma OP con otro NRO_DOC_RESP: no es la corregida (en SIGAF no pasa, pero
            // si pasara, mover la equivocada sería peor que no mover ninguna).
            new Linea(7, "C41-1-2026", "00000007/26", F2),
        ],
        [new CorreccionOpClave(1, "C41-1-2026 ", "00000009/26")]);

        Assert.AreEqual(F1, resultado["00000009/26"]);
        Assert.AreEqual(F2, resultado["00000007/26"]);
        Assert.IsFalse(resultado.ContainsKey("00000001/26"));
    }

    [TestMethod]
    public void IgnoraLineasSinFechaOSinExpediente()
    {
        var resultado = PorExpediente(
        [
            new Linea(1, "C41-1-2026", "00000001/26", null),
            new Linea(2, "C41-2-2026", null, F1),
            new Linea(3, "C41-3-2026", "", F1),
        ], []);

        Assert.AreEqual(0, resultado.Count);
    }

    [TestMethod]
    public void LaCorreccionPrevaleceSobreUnaCorreccionPosteriorDeIvc()
    {
        // IVC corrigió por su lado a otro expediente: la de SAF manda igual.
        var resultado = PorExpediente(
            [new Linea(1, "C41-1-2026", "00000005/26", F1)],
            [new CorreccionOpClave(1, "C41-1-2026", "00000009/26")]);

        Assert.AreEqual(F1, resultado["00000009/26"]);
        Assert.IsFalse(resultado.ContainsKey("00000005/26"));
    }

    [TestMethod]
    public void ElExpedienteDeLaBajadaSeLlevaALaClaveDelLedger()
    {
        // La bajada trae a veces el número sin ceros a la izquierda (y "False" en vez de
        // vacío): el resultado tiene que quedar por la clave normalizada, y las dos formas
        // del mismo expediente tienen que fundirse en un solo máximo.
        var resultado = PorExpediente(
        [
            new Linea(null, null, "1212221/26", F1),
            new Linea(null, null, "01212221/26", F2),
            new Linea(null, null, "False", F2),
        ], []);

        Assert.AreEqual(1, resultado.Count);
        Assert.AreEqual(F2, resultado["01212221/26"]);
    }
}
