#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Auditoría de cambios manuales: una fila por campo modificado en cada guardado.
/// La clave de negocio va denormalizada (sin FK) para que el historial sobreviva
/// aunque la fila original se borre. El sync con IVC no se audita: es carga masiva
/// del sistema, trazable por FechaImportacion.
/// </summary>
public class CambioAuditoria
{
    public int Id { get; set; }

    /// <summary>Agrupa los campos de un mismo guardado (un clic en Guardar = un lote).</summary>
    public Guid Lote { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Ruta de la página ("/pagos", "/caf"...), estable ante renombres de título.</summary>
    public string Vista { get; set; } = string.Empty;

    /// <summary>Id técnico de la fila (null en el tablero, que agrupa por TipoDev+NroDev).</summary>
    public int? EntidadId { get; set; }

    /// <summary>Clave legible de la fila ("Devengado DGG 307106"), para mostrar y para
    /// buscar el historial cuando no hay Id técnico.</summary>
    public string ClaveNegocio { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty; // Alta | Edición | Baja

    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
}
