#nullable enable
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace SAF.Application.Common;

/// <summary>
/// Compara dos estados de un ViewModel y produce los cambios auditables, con el
/// nombre visible de cada campo ([Display]) y los valores ya formateados para mostrar.
/// </summary>
public static class AuditoriaDiff
{
    public sealed record Cambio(string Campo, string? Antes, string? Despues);

    /// <summary>Campos que difieren entre la copia previa y lo guardado.</summary>
    public static IReadOnlyList<Cambio> Comparar<T>(T antes, T despues)
    {
        var cambios = new List<Cambio>();
        foreach (var prop in PropiedadesAuditables<T>())
        {
            var a = Formatear(prop.GetValue(antes));
            var d = Formatear(prop.GetValue(despues));
            if (!string.Equals(a, d, StringComparison.Ordinal))
                cambios.Add(new Cambio(NombreVisible(prop), a, d));
        }
        return cambios;
    }

    /// <summary>
    /// Foto de los campos con valor: para un alta (todo en "después") o una baja
    /// (todo en "antes", que es lo que permite reconstruir la fila borrada).
    /// </summary>
    public static IReadOnlyList<Cambio> Snapshot<T>(T item, bool esBaja)
    {
        var cambios = new List<Cambio>();
        foreach (var prop in PropiedadesAuditables<T>())
        {
            var valor = Formatear(prop.GetValue(item));
            if (valor is null) continue;
            cambios.Add(esBaja ? new Cambio(NombreVisible(prop), valor, null)
                               : new Cambio(NombreVisible(prop), null, valor));
        }
        return cambios;
    }

    private static IEnumerable<PropertyInfo> PropiedadesAuditables<T>() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite
                && p.GetCustomAttribute<AuditIgnoreAttribute>() is null
                && p.PropertyType != typeof(byte[])
                && (p.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(p.PropertyType)));

    private static string NombreVisible(PropertyInfo prop) =>
        prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;

    // Cultura fija: lo persistido no debe depender de la config del host (y el diff
    // compara por representación, así que un cambio de cultura generaría falsos diffs).
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>Mismo formato que muestran las grillas; vacío y null se consideran iguales.</summary>
    private static string? Formatear(object? valor) => valor switch
    {
        null => null,
        string s => s.Length == 0 ? null : s,
        // Con hora si la trae (Cargado/Revisado de CAF la editan): formatear solo la
        // fecha haría invisible al diff un cambio de hora del mismo día.
        DateTime f => f.ToString(f.TimeOfDay == TimeSpan.Zero ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm", Cultura),
        bool b => b ? "Sí" : "No",
        decimal m => m.ToString("N2", Cultura),
        _ => valor.ToString(),
    };
}
