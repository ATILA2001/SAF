#nullable enable

namespace SAF.Application.Common;

/// <summary>
/// Utilidades comunes de los validadores de fila.
/// </summary>
public static class Validaciones
{
    /// <summary>
    /// Valida el largo contra el MaxLength de la columna: sin esto, el exceso llega a
    /// SQL Server y vuelve como error de truncamiento en inglés.
    /// </summary>
    public static void Largo(List<string> errores, string? valor, int maximo, string campo)
    {
        if (valor is not null && valor.Length > maximo)
            errores.Add($"{campo} supera los {maximo} caracteres.");
    }

    /// <summary>Valida el expediente contra los formatos aceptados por ExpedienteKey.</summary>
    public static void Expediente(List<string> errores, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            errores.Add($"El expediente es obligatorio (formato: {ExpedienteKey.FormatosAceptados}).");
        else if (ExpedienteKey.Normalizar(valor) is null)
            errores.Add($"Expediente inválido: \"{valor}\". Formatos aceptados: {ExpedienteKey.FormatosAceptados}.");
    }

    /// <summary>Valida que un importe opcional, si viene cargado, no sea negativo.</summary>
    public static void ImporteNoNegativo(List<string> errores, decimal? valor, string campo)
    {
        if (valor is < 0)
            errores.Add($"{campo} no puede ser negativo.");
    }
}