using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.Caf;
using SAF.Application.Caf.Dtos;

namespace Tests.Application;

[TestClass]
public class CafValidatorTests
{
    private static CafViewModel Valida() => new()
    {
        Anio = 2026,
        Expediente = "01212221/26",
        Op = "1234",
        Beneficiario = "Proveedor",
        ImporteNeto = 100m,
    };

    [TestMethod]
    public void Fila_Valida_SinErrores()
        => Assert.AreEqual(0, CafValidator.Validar(Valida()).Count);

    [TestMethod]
    [DataRow(1999)]
    [DataRow(2101)]
    public void Anio_FueraDeRango_SeRechaza(int anio)
    {
        var vm = Valida();
        vm.Anio = anio;
        Assert.IsTrue(CafValidator.Validar(vm).Any(e => e.Contains("año")));
    }

    [TestMethod]
    public void Importes_Negativos_SeRechazan()
    {
        var vm = Valida();
        vm.ImporteNeto = -1m;
        vm.Iibb = -1m;
        Assert.AreEqual(2, CafValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Expediente_Invalido_SeRechaza()
    {
        var vm = Valida();
        vm.Expediente = "cualquier cosa";
        Assert.IsTrue(CafValidator.Validar(vm).Any(e => e.Contains("Expediente inválido")));
    }

    [TestMethod]
    public void Largos_DeColumna_SeValidan()
    {
        var vm = Valida();
        vm.Op = new string('x', 51);            // nvarchar(50)
        vm.Beneficiario = new string('x', 256); // nvarchar(255)
        vm.Cuenta = new string('x', 51);        // nvarchar(50)
        vm.CcPagadora = new string('x', 101);   // nvarchar(100)
        vm.Pase = new string('x', 101);         // nvarchar(100)
        Assert.AreEqual(5, CafValidator.Validar(vm).Count);
    }
}
