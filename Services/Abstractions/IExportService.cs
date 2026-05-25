#nullable enable

namespace SAF.Services.Abstractions;

public interface IExportService
{
    byte[] ExportToXlsx<T>(IReadOnlyList<T> data, string sheetName);
}
