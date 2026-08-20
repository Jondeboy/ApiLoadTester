using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ApiLoadTester.Reporting;

/// <summary>Owns the current page/graphics for a report being built and handles pagination:
/// callers ask EnsureSpace(height) before drawing a block and get a fresh page transparently
/// when the current one doesn't have room left.</summary>
public sealed class PdfReportContext
{
    public required PdfDocument Document { get; init; }
    public PdfPage Page { get; private set; } = null!;
    public XGraphics Gfx { get; private set; } = null!;

    public double MarginLeft { get; init; } = 40;
    public double MarginRight { get; init; } = 40;
    public double MarginTop { get; init; } = 40;
    public double MarginBottom { get; init; } = 48;

    public double CursorY { get; set; }
    public double ContentWidth => Page.Width.Point - MarginLeft - MarginRight;
    public double ContentBottom => Page.Height.Point - MarginBottom;

    public void NewPage()
    {
        Gfx?.Dispose();
        Page = Document.AddPage();
        Page.Size = PdfSharp.PageSize.A4;
        Gfx = XGraphics.FromPdfPage(Page);
        CursorY = MarginTop;
    }

    public void EnsureSpace(double height)
    {
        if (CursorY + height > ContentBottom)
            NewPage();
    }
}
