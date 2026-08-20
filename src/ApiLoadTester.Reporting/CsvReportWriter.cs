using System.Globalization;
using System.Text;
using ApiLoadTester.Core.Models;

namespace ApiLoadTester.Reporting;

/// <summary>Writes the full per-request raw results to CSV, for the user's own analysis or to hand
/// to a support engineer alongside the PDF summary.</summary>
public static class CsvReportWriter
{
    private static readonly string[] Header =
    [
        "SequenceNumber", "TimestampUtc", "LatencyMs", "StatusCode", "Status", "ErrorType", "ErrorMessage", "ResponseBytes"
    ];

    public static void Write(TestSummary summary, string path)
    {
        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        writer.WriteLine(string.Join(',', Header));

        foreach (var r in summary.RawResults)
        {
            writer.WriteLine(string.Join(',',
                r.SequenceNumber,
                r.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                r.LatencyMs.ToString("F2", CultureInfo.InvariantCulture),
                r.StatusCode?.ToString(CultureInfo.InvariantCulture) ?? "",
                r.Status,
                Quote(r.ErrorType),
                Quote(r.ErrorMessage),
                r.ResponseBytes));
        }
    }

    private static string Quote(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
