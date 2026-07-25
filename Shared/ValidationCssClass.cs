namespace SAF.Shared;

/// <summary>
/// Clase CSS que marca visualmente un campo válido/inválido en la edición inline.
/// Solo es feedback visual: la validación real está en los validadores de Application
/// y en los servicios. Mismo criterio que SAI.
/// </summary>
public static class ValidationCssClass
{
    public static string GetValidationCssClass(bool isValid) => isValid ? "valid" : "invalid";
}