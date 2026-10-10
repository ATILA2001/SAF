#nullable enable
using SAF.Application.Common;

namespace SAF.Application.ControlOp;

/// <summary>
/// En qué difiere, en palabras, el expediente que una OP tiene en la bajada del
/// expediente del devengado que se está buscando. Los errores de carga típicos son
/// chicos (otro año, un dígito, dos dígitos cambiados de lugar) y a simple vista no se
/// ven entre ocho dígitos.
/// </summary>
public static class ExpedienteDiff
{
    /// <summary>
    /// Null si alguno no es una clave financiera válida, o si difieren en todo (no hay
    /// nada que explicar). Ambos se normalizan antes: "1212221/26" y "01212221/26"
    /// son el mismo expediente.
    /// </summary>
    public static string? Describir(string? sigaf, string? objetivo)
    {
        if (ExpedienteKey.Normalizar(sigaf) is not { } a || ExpedienteKey.Normalizar(objetivo) is not { } b)
            return null;

        var (numero, anio) = (a[..8], a[9..]);
        var (numeroObj, anioObj) = (b[..8], b[9..]);

        return (numero == numeroObj, anio == anioObj) switch
        {
            (true, true) => "Es el expediente del devengado",
            (true, false) => "Mismo número, distinto año",
            (false, true) => DescribirNumero(numero, numeroObj),
            _ => null,
        };
    }

    private static string DescribirNumero(string a, string b)
    {
        var distintos = Enumerable.Range(0, a.Length).Where(i => a[i] != b[i]).ToList();
        if (distintos.Count == 1)
            return "Mismo año, difiere en un dígito";
        if (distintos.Count == 2 && distintos[1] == distintos[0] + 1
            && a[distintos[0]] == b[distintos[1]] && a[distintos[1]] == b[distintos[0]])
            return "Mismo año, dos dígitos cambiados de lugar";
        return "Mismo año, distinto número";
    }
}
