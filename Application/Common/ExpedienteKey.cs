#nullable enable
using System.Text.RegularExpressions;

namespace SAF.Application.Common;

/// <summary>
/// Helper para la clave de cruce por expediente (EE_FINANCIERA).
/// </summary>
public static class ExpedienteKey
{
    /// <summary>Texto de ayuda para la UI con los formatos aceptados.</summary>
    public const string FormatosAceptados = "01212221/26 o EX-2026-01212221-GCABA-IVC";

    /// <summary>
    /// Normaliza un expediente a la clave financiera NNNNNNNN/AA (8 dígitos).
    /// Acepta dos formatos:
    ///  - Financiera (el de las planillas de Tesorería): "1212221/26" → "01212221/26"
    ///  - SADE: "EX-2026-01212221-GCABA-IVC" → "01212221/26" (tolera '/', '-' y espacio)
    /// Devuelve null si el texto no coincide con ninguno (formato inválido).
    /// Corrige el bug de la fórmula del Excel CAF (REPT("0",8/LEN...) → 9 dígitos)
    /// unificando el formato con Devengados.
    /// </summary>
    public static string? Normalizar(string? expediente)
    {
        if (string.IsNullOrWhiteSpace(expediente)) return null;
        var s = expediente.Trim();

        // Formato financiera: NNNNNNNN/AA (número de 6-8 dígitos + año de 2 dígitos)
        var corto = Regex.Match(s, @"^(\d{6,8})\s*[/\- ]\s*(\d{2})$");
        if (corto.Success)
            return $"{corto.Groups[1].Value.PadLeft(8, '0')}/{corto.Groups[2].Value}";

        // Formato SADE: EX-2026-01212221-GCABA-IVC
        var parts = s.Split(new[] { '/', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string? anio = null, numero = null;
        foreach (var p in parts)
        {
            if (!p.All(char.IsDigit)) continue;
            if (p.Length == 4 && anio is null) anio = p;   // año (4 dígitos)
            else if (p.Length >= 6) numero = p;            // número de expediente
        }
        if (anio is null || numero is null) return null;

        return $"{numero.PadLeft(8, '0')}/{anio[2..]}";
    }
}
