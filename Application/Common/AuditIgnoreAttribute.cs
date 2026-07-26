#nullable enable

namespace SAF.Application.Common;

/// <summary>
/// Excluye una propiedad del diff de auditoría. Se marca en los Ids de lookups (se
/// audita el Nombre, no el Id) y en las columnas que el completado IVC muta en
/// segundo plano (si cambian durante una edición no fue el usuario).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditIgnoreAttribute : Attribute;
