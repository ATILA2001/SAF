#nullable enable
using ClosedXML.Excel;
using SAF.Application.Common;
using SAF.Services.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SAF.Services.Implementations;

public class ExportService : IExportService
{
    private const string FormatoFecha = "dd/MM/yyyy";
    private const string FormatoFechaHora = "dd/MM/yyyy HH:mm";
    private const string FormatoImporte = "#,##0.00";

    public byte[] ExportToXlsx<T>(IReadOnlyList<T> data, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        // Solo propiedades exportables, con el título de la planilla ([Display(Name)])
        // en lugar del nombre técnico de la propiedad.
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ExportIgnoreAttribute>() is null)
            .ToArray();

        // Header row
        for (int i = 0; i < props.Length; i++)
            ws.Cell(1, i + 1).Value = props[i].GetCustomAttribute<DisplayAttribute>()?.Name ?? props[i].Name;

        // Data rows
        for (int row = 0; row < data.Count; row++)
            for (int col = 0; col < props.Length; col++)
                Escribir(ws.Cell(row + 2, col + 1), props[col].GetValue(data[row]));

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Escribe el valor con su tipo real: como texto, en Excel los importes no suman
    /// y las fechas no se pueden filtrar por rango ni ordenar cronológicamente.
    /// </summary>
    private static void Escribir(IXLCell celda, object? valor)
    {
        switch (valor)
        {
            case null:
                break;

            case bool b:
                celda.Value = b ? "Sí" : "No";
                break;

            case DateTime fecha:
                celda.Value = fecha;
                // Las columnas de auditoría (Cargado, Revisado) llevan hora; el resto no.
                celda.Style.NumberFormat.Format =
                    fecha.TimeOfDay == TimeSpan.Zero ? FormatoFecha : FormatoFechaHora;
                break;

            case decimal importe:
                celda.Value = importe;
                celda.Style.NumberFormat.Format = FormatoImporte;
                break;

            case double d:
                celda.Value = d;
                celda.Style.NumberFormat.Format = FormatoImporte;
                break;

            case int entero:
                celda.Value = entero;
                break;

            case string texto:
                celda.Value = texto;
                break;

            default:
                celda.Value = valor.ToString();
                break;
        }
    }
}