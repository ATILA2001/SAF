using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.StatusContabilidad;
using SAF.Application.StatusContabilidad.Dtos;

namespace Tests.Application;

[TestClass]
public class StatusContabilidadValidatorTests
{
    [TestMethod]
    public void Fila_SinDatosManuales_EsValida()
        => Assert.AreEqual(0, StatusContabilidadValidator.Validar(new StatusContabilidadViewModel()).Count);

    [TestMethod]
    public void Largos_EnElLimite_SonValidos()
    {
        var vm = new StatusContabilidadViewModel
        {
            ObservacionesCuentasPagar = new string('x', 500),
            ObservacionesLiquidaciones = new string('x', 500),
            SinFacturaMotivo = new string('x', 20),
        };
        Assert.AreEqual(0, StatusContabilidadValidator.Validar(vm).Count);
    }

    [TestMethod]
    public void Largos_Excedidos_UnErrorPorCampo()
    {
        var vm = new StatusContabilidadViewModel
        {
            ObservacionesCuentasPagar = new string('x', 501),
            ObservacionesLiquidaciones = new string('x', 501),
            SinFacturaMotivo = new string('x', 21),
        };
        Assert.AreEqual(3, StatusContabilidadValidator.Validar(vm).Count);
    }
}
