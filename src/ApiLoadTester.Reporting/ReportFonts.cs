using PdfSharp.Drawing;

namespace ApiLoadTester.Reporting;

/// <summary>
/// Font helper for the PDF report. PdfSharp 6 requires either an explicit IFontResolver or
/// GlobalFontSettings.UseWindowsFontsUnderWindows = true (set once at app startup) plus a real,
/// installed Windows font family name - it does not map the classic PDF base-14 names ("Helvetica",
/// "Courier") automatically. Since this app is Windows-only, we use "Segoe UI" / "Consolas", which
/// ship with every supported Windows version, avoiding any need to embed or redistribute font files.
/// </summary>
public static class ReportFonts
{
    private const string Sans = "Segoe UI";
    private const string Mono = "Consolas";

    public static XFont Regular(double size) => new(Sans, size, XFontStyleEx.Regular);
    public static XFont Bold(double size) => new(Sans, size, XFontStyleEx.Bold);
    public static XFont Italic(double size) => new(Sans, size, XFontStyleEx.Italic);
    public static XFont Monospace(double size) => new(Mono, size, XFontStyleEx.Regular);
}
