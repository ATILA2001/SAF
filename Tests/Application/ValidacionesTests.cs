using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.Common;

namespace Tests.Application;

[TestClass]
public class ValidacionesTests
{
    [TestMethod]
    public void Largo_ExcedeElMaximo_AgregaError()
    {
        var errores = new List<string>();
        Validaciones.Largo(errores, new string('x', 11), 10, "Campo");
        Assert.AreEqual(1, errores.Count);
        StringAssert.Contains(errores[0], "Campo");
        StringAssert.Contains(errores[0], "10");
    }

    [TestMethod]
    public void Largo_EnElLimiteONull_NoAgregaError()
    {
        var errores = new List<string>();
        Validaciones.Largo(errores, new string('x', 10), 10, "Campo");
        Validaciones.Largo(errores, null, 10, "Campo");
        Assert.AreEqual(0, errores.Count);
    }

    [TestMethod]
    public void Expediente_Vacio_EsObligatorio()
    {
        var errores = new List<string>();
        Validaciones.Expediente(errores, "  ");
        Assert.AreEqual(1, errores.Count);
        StringAssert.Contains(errores[0], "obligatorio");
    }

    [TestMethod]
    public void Expediente_Invalido_AgregaErrorConElValor()
    {
        var errores = new List<string>();
        Validaciones.Expediente(errores, "no-es-expediente");
        Assert.AreEqual(1, errores.Count);
        StringAssert.Contains(errores[0], "no-es-expediente");
    }

    [TestMethod]
    public void Expediente_Valido_NoAgregaError()
    {
        var errores = new List<string>();
        Validaciones.Expediente(errores, "01212221/26");
        Assert.AreEqual(0, errores.Count);
    }

    [TestMethod]
    public void ImporteNoNegativo_SoloRechazaNegativos()
    {
        var errores = new List<string>();
        Validaciones.ImporteNoNegativo(errores, -0.01m, "Importe");
        Assert.AreEqual(1, errores.Count);

        errores.Clear();
        Validaciones.ImporteNoNegativo(errores, 0m, "Importe");
        Validaciones.ImporteNoNegativo(errores, null, "Importe");
        Validaciones.ImporteNoNegativo(errores, 100m, "Importe");
        Assert.AreEqual(0, errores.Count);
    }
}
