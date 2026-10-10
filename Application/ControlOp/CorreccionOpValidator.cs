#nullable enable
using SAF.Application.Common;
using SAF.Application.ControlOp.Dtos;

namespace SAF.Application.ControlOp;

/// <summary>
/// Reglas de una corrección de OP. Fuente única: la usa el servicio (integridad, no se
/// puede saltear) y la grilla (aviso antes de perder lo cargado). La existencia de la OP
/// en SIGAF la verifica el servicio, que es quien puede consultar IVC.
/// </summary>
public static class CorreccionOpValidator
{
    public const int MaxLargoOp = 80;

    public static IReadOnlyList<string> Validar(CorreccionOpViewModel vm)
    {
        var errores = new List<string>();

        if (vm.NroDocResp <= 0)
            errores.Add("NRO DOC RESP es obligatorio (el número de expediente que figura en la bajada de OP).");

        if (string.IsNullOrWhiteSpace(vm.Op))
            errores.Add("La OP es obligatoria (ej. C41-356190-2026).");
        else
            Validaciones.Largo(errores, vm.Op, MaxLargoOp, "OP");

        Validaciones.Expediente(errores, vm.ExpedienteCorregido);

        return errores;
    }
}
