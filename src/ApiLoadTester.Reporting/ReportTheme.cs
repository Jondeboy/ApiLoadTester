using OxyPlot;
using PdfSharp.Drawing;

namespace ApiLoadTester.Reporting;

/// <summary>Chart colors (OxyPlot), used by ChartImageRenderer when rasterizing report chart images.</summary>
public static class ChartTheme
{
    public static readonly OxyColor Primary = OxyColor.FromRgb(0x2E, 0x5C, 0xE6);
    public static readonly OxyColor Success = OxyColor.FromRgb(0x1E, 0xA0, 0x5A);
    public static readonly OxyColor Error = OxyColor.FromRgb(0xD6, 0x33, 0x3C);
    public static readonly OxyColor GridLine = OxyColor.FromRgb(0xE0, 0xE0, 0xE0);
    public static readonly OxyColor Text = OxyColor.FromRgb(0x22, 0x22, 0x22);

    public const int ChartPixelWidth = 1400;
    public const int ChartPixelHeight = 560;
}

/// <summary>PDF document colors (PdfSharp), matching ChartTheme's palette, used by
/// SimpleTableRenderer and PdfReportBuilder when drawing document chrome/tables.</summary>
public static class PdfTheme
{
    public static readonly XColor Primary = XColor.FromArgb(0x2E, 0x5C, 0xE6);
    public static readonly XColor Success = XColor.FromArgb(0x1E, 0xA0, 0x5A);
    public static readonly XColor Error = XColor.FromArgb(0xD6, 0x33, 0x3C);
    public static readonly XColor Warning = XColor.FromArgb(0xE0, 0x8E, 0x0B);
    public static readonly XColor MutedText = XColor.FromArgb(0x66, 0x66, 0x66);
    public static readonly XColor BodyText = XColor.FromArgb(0x22, 0x22, 0x22);
    public static readonly XColor RowAlt = XColor.FromArgb(0xFA, 0xFA, 0xFA);

    public static readonly XBrush PrimaryBrush = new XSolidBrush(Primary);
    public static readonly XBrush ErrorBrush = new XSolidBrush(Error);
    public static readonly XBrush WarningBrush = new XSolidBrush(Warning);
    public static readonly XBrush MutedBrush = new XSolidBrush(MutedText);
    public static readonly XBrush BodyBrush = new XSolidBrush(BodyText);
}
