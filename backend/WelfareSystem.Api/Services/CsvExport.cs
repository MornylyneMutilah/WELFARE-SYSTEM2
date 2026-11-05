using System.Text;
namespace WelfareSystem.Api.Services;

public static class CsvExport
{
    public static string Cell(string? value) {
        value ??= "";
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    public static byte[] Bytes(IEnumerable<IEnumerable<string?>> rows) => Encoding.UTF8.GetPreamble()
        .Concat(Encoding.UTF8.GetBytes(string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(Cell)))))).ToArray();
}
