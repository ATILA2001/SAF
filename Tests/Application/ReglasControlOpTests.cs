using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.ControlOp;

namespace Tests.Application;

/// <summary>
/// La lista de control es la fórmula de la hoja "control OP":
///   (FechaPagoTotal = "") * (Status &lt;&gt; "anulado") * (Status &lt;&gt; "avanzar CAF")
///   * (FechaFirmaOp &lt;&gt; "") * (FechaFirmaOp &lt; A1), con A1 = hoy - 3 días.
/// Un término de más o de menos cambia qué devengados se revisan a mano.
/// </summary>
[TestClass]
public class ReglasControlOpTests
{
    private static readonly DateTime Hoy = new(2026, 10, 2);
    private static readonly DateTime Corte = ReglasControlOp.FechaCorte(Hoy);

    [TestMethod]
    public void LaFechaDeCorteEsHoyMenosTresDias()
    {
        Assert.AreEqual(new DateTime(2026, 9, 29), Corte);
        // Sin hora: la comparación con la firma (que es fecha pura) no puede depender de la hora.
        Assert.AreEqual(TimeSpan.Zero, ReglasControlOp.FechaCorte(new DateTime(2026, 10, 2, 15, 30, 0)).TimeOfDay);
    }

    [TestMethod]
    public void EntraConFirmaAnteriorAlCorteSinPagoYStatusComun()
    {
        Assert.IsTrue(ReglasControlOp.EstaPendiente(new DateTime(2026, 9, 20), "avanzar", null, Corte));
        // Sin status también entra: el Excel solo excluye dos valores concretos.
        Assert.IsTrue(ReglasControlOp.EstaPendiente(new DateTime(2026, 9, 20), null, null, Corte));
    }

    [TestMethod]
    public void NoEntraSinFirmaOConFirmaReciente()
    {
        Assert.IsFalse(ReglasControlOp.EstaPendiente(null, "avanzar", null, Corte));
        // Firmada el mismo día del corte: I < A1 es estricto.
        Assert.IsFalse(ReglasControlOp.EstaPendiente(Corte, "avanzar", null, Corte));
        Assert.IsFalse(ReglasControlOp.EstaPendiente(Hoy, "avanzar", null, Corte));
    }

    [TestMethod]
    public void NoEntraSiYaTieneFechaPagoTotal()
    {
        Assert.IsFalse(ReglasControlOp.EstaPendiente(new DateTime(2026, 9, 20), "avanzar", new DateTime(2026, 9, 25), Corte));
    }

    [TestMethod]
    public void NoEntranAnuladosNiAvanzarCafSinDistinguirMayusculas()
    {
        Assert.IsFalse(ReglasControlOp.EstaPendiente(new DateTime(2026, 9, 20), "anulado", null, Corte));
        Assert.IsFalse(ReglasControlOp.EstaPendiente(new DateTime(2026, 9, 20), "ANULADO", null, Corte));
        Assert.IsFalse(ReglasControlOp.EstaPendiente(new DateTime(2026, 9, 20), "avanzar CAF", null, Corte));
        Assert.IsFalse(ReglasControlOp.EstaPendiente(new DateTime(2026, 9, 20), "Avanzar caf", null, Corte));
        // "no avanzar CAF" no es "avanzar CAF": entra.
        Assert.IsTrue(ReglasControlOp.EstaPendiente(new DateTime(2026, 9, 20), "no avanzar CAF", null, Corte));
    }
}
