#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Miembros comunes de las tablas "extra" (datos cargados a mano sobre una vista):
/// las marcas de auditoría y la versión de concurrencia que exige el upsert compartido
/// (<see cref="SAF.Repositories.Implementations.ExtraRepositoryBase{TEntity}"/>).
/// </summary>
public interface IExtraEditable
{
    DateTime FechaCreacion { get; set; }
    DateTime FechaModificacion { get; set; }

    /// <summary>Control de concurrencia optimista: SQL Server la actualiza en cada UPDATE.</summary>
    byte[]? RowVersion { get; set; }
}
