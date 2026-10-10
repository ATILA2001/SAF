using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.ControlOp;

namespace Tests.Application;

/// <summary>
/// El cruce con SIGAF_OP se hace en SQL contra EE_FINANCIERA tal cual viene, y la bajada
/// trae a veces el número sin ceros a la izquierda: para que una OP pagada no quede
/// "sin fecha" (y el devengado caiga en Control OP sin motivo) hay que pedir las dos formas.
/// </summary>
[TestClass]
public class ExpedienteBajadaTests
{
    [TestMethod]
    public void LasVariantesIncluyenLaClaveYElNumeroSinCeros()
    {
        CollectionAssert.AreEqual(new[] { "01212221/26", "1212221/26" },
            ExpedienteBajada.VariantesEnBajada("01212221/26").ToArray());
    }

    [TestMethod]
    public void SinCerosALaIzquierdaNoDuplica()
    {
        CollectionAssert.AreEqual(new[] { "12122210/26" },
            ExpedienteBajada.VariantesEnBajada("12122210/26").ToArray());
    }

    [TestMethod]
    public void LoQueNoTieneFormaDeClaveVuelveTalCual()
    {
        CollectionAssert.AreEqual(new[] { "EX-2026" }, ExpedienteBajada.VariantesEnBajada("EX-2026").ToArray());
    }

    [TestMethod]
    public void NormalizarTrataFalseComoVacioYCompletaLosCeros()
    {
        Assert.IsNull(ExpedienteBajada.Normalizar("False"));
        Assert.IsNull(ExpedienteBajada.Normalizar("  "));
        Assert.AreEqual("01212221/26", ExpedienteBajada.Normalizar("1212221/26"));
    }
}
