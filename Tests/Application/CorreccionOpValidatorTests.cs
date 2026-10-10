using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;

namespace Tests.Application;

/// <summary>
/// Una corrección sin clave completa o con un expediente que no normaliza nunca
/// cruzaría con nada: se rechaza antes de llegar a la base.
/// </summary>
[TestClass]
public class CorreccionOpValidatorTests
{
    private static CorreccionOpViewModel Valida() => new()
    {
        NroDocResp = 40076624,
        Op = "C41-356190-2026",
        ExpedienteCorregido = "1212221/26",
    };

    [TestMethod]
    public void UnaCorreccionCompletaEsValida()
    {
        Assert.AreEqual(0, CorreccionOpValidator.Validar(Valida()).Count);
    }

    [TestMethod]
    public void ExigeNroDocRespYOp()
    {
        var sinNro = Valida();
        sinNro.NroDocResp = 0;
        Assert.IsTrue(CorreccionOpValidator.Validar(sinNro).Any(e => e.Contains("NRO DOC RESP")));

        var sinOp = Valida();
        sinOp.Op = "  ";
        Assert.IsTrue(CorreccionOpValidator.Validar(sinOp).Any(e => e.Contains("OP")));

        var opLarga = Valida();
        opLarga.Op = new string('X', CorreccionOpValidator.MaxLargoOp + 1);
        Assert.IsTrue(CorreccionOpValidator.Validar(opLarga).Any(e => e.Contains("supera")));
    }

    [TestMethod]
    public void ExigeUnExpedienteQueNormalice()
    {
        var vacio = Valida();
        vacio.ExpedienteCorregido = "";
        Assert.IsTrue(CorreccionOpValidator.Validar(vacio).Any(e => e.Contains("obligatorio")));

        var invalido = Valida();
        invalido.ExpedienteCorregido = "expediente 12";
        Assert.IsTrue(CorreccionOpValidator.Validar(invalido).Any(e => e.Contains("inválido")));

        // El formato SADE también vale: se normaliza al guardar.
        var sade = Valida();
        sade.ExpedienteCorregido = "EX-2026-01212221-GCABA-IVC";
        Assert.AreEqual(0, CorreccionOpValidator.Validar(sade).Count);
    }
}
