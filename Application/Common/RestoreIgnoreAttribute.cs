#nullable enable

namespace SAF.Application.Common;

/// <summary>
/// Excluye una propiedad de la copia/restauración que hace la grilla al editar y
/// cancelar. Se marca en las columnas que muta el completado IVC en segundo plano:
/// el usuario no puede editarlas, y restaurarlas al cancelar las devolvería al
/// estado pre-completado (vacías) hasta la próxima recarga.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RestoreIgnoreAttribute : Attribute;
