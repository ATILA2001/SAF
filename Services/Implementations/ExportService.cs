#nullable enable
using ClosedXML.Excel;
using SAF.Services.Abstractions;
using System.Reflection;

namespace SAF.Services.Implementations;

public class ExportService : IExportService
{
    public byte[] ExportToXlsx<T>(IReadOnlyList<T> data, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Header row
        for (int i = 0; i < props.Length; i++)
            ws.Cell(1, i + 1).Value = props[i].Name;

        // Data rows
        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < props.Length; col++)
            {
                var val = props[col].GetValue(data[row]);
                ws.Cell(row + 2, col + 1).Value = val switch
                {
                    null => string.Empty,
                    bool b => b ? "Sí" : "No",
                    DateTime dt => dt.ToString("dd/MM/yyyy"),
                    _ => val.ToString() ?? string.Empty
                };
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
