using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.Pagos;
using SAF.Application.Pagos.Dtos;

namespace Tests.Application;

[TestClass]
public class PagoValidatorTests
{
    private static PagoViewModel AltaValida() => new()
    {
        TipoDev = "PRD",
        NroDev = 123,
        FechaDevengado = new DateTime(2026, 7, 1),
        Importe = 1000m,
        Expediente = "01212221/26",
        Empresa = "Empresa de prueba",
    };

    [TestMethod]
    public void Alta_Valida_SinErrores()
        => Assert.AreEqual(0, PagoValidator.ValidarAlta(AltaValida()).Count);

    [TestMethod]
    public void Alta_SinTipo_EsObligatorio()
    {
        var vm = AltaValida();
        vm.TipoDev = "  ";
        var errores = PagoValidator.ValidarAlta(vm);
        Assert.IsTrue(errores.Any(e => e.Contains("obligatorio")));
    }

    [TestMethod]
    [DataRow("C55")]
    [DataRow("cps")] // se normaliza a mayúsculas antes de comparar
    public void Alta_TipoExcluido_MismoFiltroQueElSync(string tipo)
    {
        var vm = AltaValida();
        vm.TipoDev = tipo;
        var errores = PagoValidator.ValidarAlta(vm);
        Assert.IsTrue(errores.Any(e => e.Contains("excluido")));
    }

    [TestMethod]
    public void Alta_TipoDemasiadoLargo_SeRechaza()
    {
        var vm = AltaValida();
        vm.TipoDev = new string('X', 21);
        var errores = PagoValidator.ValidarAlta(vm);
        Assert.IsTrue(errores.Any(e => e.Contains("20 caracteres")));
    }

    [TestMethod]
    public void Alta_SinNumeroFechaOImporte_UnErrorPorCampo()
    {
        var vm = AltaValida();
        vm.NroDev = 0;
        vm.FechaDevengado = null;
        vm.Importe = null;
        var errores = PagoValidator.ValidarAlta(vm);
        Assert.AreEqual(3, errores.Count);
    }

    [TestMethod]
    public void Alta_ImporteCero_SeRechaza_MismoFiltroQueElSync()
    {
        var vm = AltaValida();
        vm.Importe = 0m;
        Assert.IsTrue(PagoValidator.ValidarAlta(vm).Any(e => e.Contains("mayor a cero")));
    }

    [TestMethod]
    public void Alta_EmpresaLarga_SeRechaza()
    {
        var vm = AltaValida();
        vm.Empresa = new string('x', 256);
        Assert.IsTrue(PagoValidator.ValidarAlta(vm).Any(e => e.Contains("Empresa")));
    }

    [TestMethod]
    public void Edicion_LargosDeColumna_CoincidenConSql()
    {
        // Observaciones nvarchar(500) y Ccoo nvarchar(200): el validador es la fuente
        // única que evita que el exceso llegue a SQL como error de truncamiento.
        var enElLimite = new PagoViewModel { Observaciones = new string('x', 500), Ccoo = new string('x', 200) };
        Assert.AreEqual(0, PagoValidator.ValidarEdicion(enElLimite).Count);

        var excedido = new PagoViewModel { Observaciones = new string('x', 501), Ccoo = new string('x', 201) };
        Assert.AreEqual(2, PagoValidator.ValidarEdicion(excedido).Count);
    }

    [TestMethod]
    public void Alta_IncluyeLasReglasDeEdicion()
    {
        var vm = AltaValida();
        vm.Observaciones = new string('x', 501);
        Assert.IsTrue(PagoValidator.ValidarAlta(vm).Any(e => e.Contains("Observaciones")));
    }
}
