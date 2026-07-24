#nullable enable

namespace SAF.Application.Common;

/// <summary>
/// Excluye una propiedad de la exportación a Excel (ExportService).
/// Usar en IDs internos y campos técnicos que no aportan al usuario.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ExportIgnoreAttribute : Attribute;
