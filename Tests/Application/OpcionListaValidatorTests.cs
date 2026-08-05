using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.AdminListas;
using SAF.Application.AdminListas.Dtos;

namespace Tests.Application;

[TestClass]
public class OpcionListaValidatorTests
{
    private static OpcionListaViewModel Valida() => new()
    {
        Nombre = "avanzar",
        Orden = 1,
        Activo = true,
    };

    [TestMethod]
    public void Fila_Valida_SinErrores()
        => Assert.AreEqual(0, OpcionListaValidator.Validar(Valida()).Count);

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Nombre_Vacio_SeRechaza(string nombre)
    {
        var vm = Valida();
        vm.Nombre = nombre;
        Assert.IsTrue(OpcionListaValidator.Validar(vm).Any(e => e.Contains("nombre es obligatorio")));
    }

    [TestMethod]
    public void Nombre_Largo_SeRechaza()
    {
        var vm = Valida();
        vm.Nombre = new string('x', OpcionListaValidator.NombreMaxLength + 1);
        Assert.IsTrue(OpcionListaValidator.Validar(vm).Any(e => e.Contains("supera")));
    }

    [TestMethod]
    public void Orden_Negativo_SeRechaza()
    {
        var vm = Valida();
        vm.Orden = -1;
        Assert.IsTrue(OpcionListaValidator.Validar(vm).Any(e => e.Contains("orden")));
    }
}
