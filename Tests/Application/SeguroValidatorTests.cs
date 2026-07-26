using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.Seguros;
using SAF.Application.Seguros.Dtos;

namespace Tests.Application;

[TestClass]
public class SeguroValidatorTests
{
    private static SeguroViewModel Valida() => new()
    {
        Expediente = "01212221/26",
        SeguroOpcionId = 1,
        Beneficiario = "Proveedor",
        ImporteNeto = 100m,
    };

    [TestMethod]
    public void Fila_Valida_SinErrores()
        => Assert.AreEqual(0, SeguroValidator.Validar(Valida()).Count);

    [TestMethod]
    [DataRow(null)]
    [DataRow(0)]
    public void SinEstadoDeSeguro_SeRechaza_PorElCruceConPagos(int? opcionId)
    {
        // Sin estado la fila no participa del "peor caso gana" de Pagos: quedaría
        // cargada pero invisible para la vista que la necesita.
        var vm = Valida();
        vm.SeguroOpcionId = opcionId;
        Assert.IsTrue(SeguroValidator.Validar(vm).Any(e => e.Contains("estado del seguro")));
    }

    [TestMethod]
    public void Largos_DeColumna_SeValidan()
    {
        var vm = Valida();
        vm.Op = new string('x', 51);            // nvarchar(50)
        vm.Beneficiario = new string('x', 256); // nvarchar(255)
        vm.Estado = new string('x', 101);       // nvarchar(100)
        Assert.AreEqual(3, SeguroValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void ImporteNegativo_SeRechaza()
    {
        var vm = Valida();
        vm.ImporteNeto = -1m;
        Assert.AreEqual(1, SeguroValidator.Validar(vm).Count);
    }
}
