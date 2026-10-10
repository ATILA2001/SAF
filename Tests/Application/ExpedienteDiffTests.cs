using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.ControlOp;

namespace Tests.Application;

/// <summary>
/// El diálogo de búsqueda dice en palabras en qué difiere el expediente de la OP del
/// buscado: los errores de carga típicos (otro año, un dígito, dos dígitos cambiados)
/// no se ven a ojo entre ocho dígitos, y describirlos mal mandaría al usuario a
/// asignar la OP equivocada.
/// </summary>
[TestClass]
public class ExpedienteDiffTests
{
    [TestMethod]
    public void DescribeElAnioDistinto()
    {
        Assert.AreEqual("Mismo número, distinto año", ExpedienteDiff.Describir("25428985/26", "25428985/25"));
    }

    [TestMethod]
    public void DescribeUnDigitoYDosCambiadosDeLugar()
    {
        Assert.AreEqual("Mismo año, difiere en un dígito", ExpedienteDiff.Describir("25428985/25", "25428995/25"));
        Assert.AreEqual("Mismo año, dos dígitos cambiados de lugar", ExpedienteDiff.Describir("25428985/25", "25429885/25"));
        Assert.AreEqual("Mismo año, distinto número", ExpedienteDiff.Describir("03731039/25", "25428985/25"));
    }

    [TestMethod]
    public void NormalizaAntesDeComparar()
    {
        // La bajada puede traer el número sin ceros a la izquierda: es el mismo expediente.
        Assert.AreEqual("Es el expediente del devengado", ExpedienteDiff.Describir("3731039/25", "03731039/25"));
    }

    [TestMethod]
    public void SinNadaQueExplicarDevuelveNull()
    {
        Assert.IsNull(ExpedienteDiff.Describir("03731039/26", "25428985/25"), "difieren en todo");
        Assert.IsNull(ExpedienteDiff.Describir(null, "25428985/25"));
        Assert.IsNull(ExpedienteDiff.Describir("False", "25428985/25"));
        Assert.IsNull(ExpedienteDiff.Describir("25428985/25", ""));
    }
}
