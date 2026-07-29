using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.Common;
using SAF.Application.Pagos.Dtos;

namespace Tests.Application;

[TestClass]
public class AuditoriaDiffTests
{
    [TestMethod]
    public void Comparar_DetectaCambios_ConNombreVisibleYFormato()
    {
        var antes = new PagoViewModel
        {
            Observaciones = "original",
            FechaFirmaOp = new DateTime(2026, 7, 1),
            Importe = 1000m,
        };
        var despues = new PagoViewModel
        {
            Observaciones = "editada",
            FechaFirmaOp = new DateTime(2026, 7, 15),
            Importe = 1000m,
        };

        var cambios = AuditoriaDiff.Comparar(antes, despues);

        Assert.AreEqual(2, cambios.Count);
        var obs = cambios.Single(c => c.Campo == "OBSERVACIONES");
        Assert.AreEqual("original", obs.Antes);
        Assert.AreEqual("editada", obs.Despues);
        var firma = cambios.Single(c => c.Campo == "FECHA FIRMA OP");
        Assert.AreEqual("01/07/2026", firma.Antes);
        Assert.AreEqual("15/07/2026", firma.Despues);
    }

    [TestMethod]
    public void Comparar_IgnoraIdsDeLookupsRowVersionYColecciones()
    {
        // El Id del lookup cambia junto con el Nombre: solo el Nombre debe auditarse.
        var antes = new PagoViewModel { StatusDgayfOpcionId = 1, StatusDgayfNombre = "avanzar", RowVersion = [1] };
        var despues = new PagoViewModel
        {
            StatusDgayfOpcionId = 2,
            StatusDgayfNombre = "avanzar CAF",
            RowVersion = [2],
            CafLineas = new List<SAF.Application.Caf.Dtos.LineaCafViewModel> { new() { Op = "1" } },
        };

        var cambios = AuditoriaDiff.Comparar(antes, despues);

        Assert.AreEqual(1, cambios.Count);
        Assert.AreEqual("STATUS DGAyF", cambios[0].Campo);
    }

    [TestMethod]
    public void Comparar_LasColumnasQueMutaElCompletadoIvc_NoSeAuditan()
    {
        // Si el completado corre durante una edición, esos cambios no son del usuario.
        var antes = new PagoViewModel();
        var despues = new PagoViewModel
        {
            FechaSade = DateTime.Today,
            BuzonSade = "IVC-DGTES",
            FechaDePagoNoCaf = DateTime.Today,
            FechaPagoTotal = DateTime.Today,
        };

        Assert.AreEqual(0, AuditoriaDiff.Comparar(antes, despues).Count);
    }

    [TestMethod]
    public void Comparar_VacioYNull_SonEquivalentes()
    {
        var antes = new PagoViewModel { Observaciones = "" };
        var despues = new PagoViewModel { Observaciones = null };
        Assert.AreEqual(0, AuditoriaDiff.Comparar(antes, despues).Count);
    }

    [TestMethod]
    public void Snapshot_Alta_PoneLosValoresEnDespues()
    {
        var item = new PagoViewModel { TipoDev = "PRD", NroDev = 5, CafSiNo = true };
        var cambios = AuditoriaDiff.Snapshot(item, esBaja: false);

        Assert.IsTrue(cambios.All(c => c.Antes is null && c.Despues is not null));
        Assert.AreEqual("PRD", cambios.Single(c => c.Campo == "TIPO DEV").Despues);
        Assert.AreEqual("Sí", cambios.Single(c => c.Campo == "CafSiNo").Despues);
    }

    [TestMethod]
    public void Snapshot_Baja_PoneLosValoresEnAntes()
    {
        var item = new PagoViewModel { TipoDev = "PRD", Importe = 1234.5m };
        var cambios = AuditoriaDiff.Snapshot(item, esBaja: true);

        Assert.IsTrue(cambios.All(c => c.Despues is null && c.Antes is not null));
        // Cultura fija es-AR: lo persistido no depende de la config del host.
        Assert.AreEqual("1.234,50", cambios.Single(c => c.Campo == "IMPORTE").Antes);
    }

    [TestMethod]
    public void Comparar_DetectaCambiosDeHora_EnFechasConHora()
    {
        // Cargado/Revisado de CAF editan fecha Y hora: el mismo día con otra hora
        // tiene que generar diff (formatear solo la fecha lo hacía invisible).
        var antes = new SAF.Application.Caf.Dtos.CafViewModel { Cargado = new DateTime(2026, 7, 25, 9, 0, 0) };
        var despues = new SAF.Application.Caf.Dtos.CafViewModel { Cargado = new DateTime(2026, 7, 25, 15, 30, 0) };

        var cambios = AuditoriaDiff.Comparar(antes, despues);

        Assert.AreEqual(1, cambios.Count);
        Assert.AreEqual("25/07/2026 09:00", cambios[0].Antes);
        Assert.AreEqual("25/07/2026 15:30", cambios[0].Despues);
    }
}
