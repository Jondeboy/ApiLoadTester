using PdfSharp.Drawing;

namespace ApiLoadTester.Reporting;

/// <summary>Small hand-rolled table drawer for PdfSharp - the MIT "plain" PdfSharp package has no
/// high-level flow/table API, so grid layout, borders, and pagination are done here directly.</summary>
public static class SimpleTableRenderer
{
    private const double RowHeight = 20;
    private const double CellPadding = 5;

    private static readonly XBrush HeaderFill = PdfTheme.PrimaryBrush;
    private static readonly XBrush HeaderText = XBrushes.White;
    private static readonly XBrush RowFill = XBrushes.White;
    private static readonly XBrush RowAltFill = new XSolidBrush(PdfTheme.RowAlt);

    /// <summary>columnWidthRatios must sum to 1.0 and is scaled against the context's content width.</summary>
    public static void DrawTable(
        PdfReportContext ctx,
        string[] headers,
        IReadOnlyList<string[]> rows,
        double[] columnWidthRatios,
        XFont? headerFont = null,
        XFont? cellFont = null)
    {
        headerFont ??= ReportFonts.Bold(9.5);
        cellFont ??= ReportFonts.Regular(9.5);

        var widths = columnWidthRatios.Select(r => r * ctx.ContentWidth).ToArray();

        DrawRow(ctx, headers, widths, headerFont, HeaderText, HeaderFill);

        var alt = false;
        foreach (var row in rows)
        {
            var startedNewPage = ctx.CursorY + RowHeight > ctx.ContentBottom;
            ctx.EnsureSpace(RowHeight);
            if (startedNewPage)
                DrawRow(ctx, headers, widths, headerFont, HeaderText, HeaderFill);

            DrawRow(ctx, row, widths, cellFont, PdfTheme.BodyBrush, alt ? RowAltFill : RowFill);
            alt = !alt;
        }

        ctx.CursorY += 4;
    }

    private static void DrawRow(PdfReportContext ctx, string[] cells, double[] widths, XFont font, XBrush textBrush, XBrush fillBrush)
    {
        ctx.EnsureSpace(RowHeight);

        var x = ctx.MarginLeft;
        var y = ctx.CursorY;
        var rowRect = new XRect(x, y, widths.Sum(), RowHeight);

        ctx.Gfx.DrawRectangle(fillBrush, rowRect);
        ctx.Gfx.DrawRectangle(XPens.LightGray, rowRect);

        var cellX = x;
        for (var i = 0; i < cells.Length && i < widths.Length; i++)
        {
            var cellRect = new XRect(cellX + CellPadding, y, widths[i] - (2 * CellPadding), RowHeight);
            var text = Elide(ctx.Gfx, cells[i] ?? "", font, cellRect.Width);
            ctx.Gfx.DrawString(text, font, textBrush, cellRect, XStringFormats.CenterLeft);

            if (i < widths.Length - 1)
                ctx.Gfx.DrawLine(XPens.LightGray, cellX + widths[i], y, cellX + widths[i], y + RowHeight);

            cellX += widths[i];
        }

        ctx.CursorY += RowHeight;
    }

    private static string Elide(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        if (gfx.MeasureString(text, font).Width <= maxWidth)
            return text;

        const string ellipsis = "...";
        var truncated = text;
        while (truncated.Length > 0 && gfx.MeasureString(truncated + ellipsis, font).Width > maxWidth)
            truncated = truncated[..^1];

        return truncated.Length == 0 ? ellipsis : truncated + ellipsis;
    }
}
