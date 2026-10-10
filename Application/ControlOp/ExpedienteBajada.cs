#nullable enable
using SAF.Application.Common;

namespace SAF.Application.ControlOp;

/// <summary>
/// El expediente tal como lo trae la bajada de OP, llevado a la clave con la que SAF
/// cruza todo (NNNNNNNN/AA). La bajada deja "False" (artefacto de la fórmula del
/// Excel) en celdas sin expediente, y trae el número sin ceros a la izquierda en
/// algunos casos: ninguna de las dos formas cruzaría con el ledger tal cual.
/// </summary>
public static class ExpedienteBajada
{
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Equals("False", StringComparison.OrdinalIgnoreCase))
            return null;
        return ExpedienteKey.Normalizar(valor) ?? valor.Trim();
    }

    /// <summary>
    /// Las formas en que una clave del ledger puede aparecer en EE_FINANCIERA: tal cual
    /// y con el número sin ceros a la izquierda ("01212221/26" y "1212221/26"). El cruce
    /// en SQL se hace contra el valor crudo de la bajada, así que hay que pedir todas.
    /// </summary>
    public static IEnumerable<string> VariantesEnBajada(string clave)
    {
        yield return clave;

        var barra = clave.IndexOf('/');
        if (barra <= 0) yield break;

        var numero = clave[..barra].TrimStart('0');
        if (numero.Length == 0) numero = "0";
        var sinCeros = numero + clave[barra..];
        if (!string.Equals(sinCeros, clave, StringComparison.Ordinal))
            yield return sinCeros;
    }
}
