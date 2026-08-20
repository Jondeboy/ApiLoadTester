using PdfSharp.Fonts;

namespace ApiLoadTester.Reporting;

/// <summary>
/// Minimal, deterministic IFontResolver that reads a small fixed set of font files straight out of
/// the Windows Fonts folder. PdfSharp 6 dropped automatic OS font resolution and its own built-in
/// Windows resolver depends on Windows font-enumeration APIs that aren't reliably available outside
/// a full desktop session; reading known files by path sidesteps that entirely. This app is
/// Windows-only, and Segoe UI/Consolas ship with every supported Windows version, so no font files
/// are embedded or redistributed - we only ever read what the OS already provides.
/// </summary>
public sealed class WindowsFontResolver : IFontResolver
{
    private const string SansFamily = "Segoe UI";
    private const string MonoFamily = "Consolas";

    private static readonly string FontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    private static readonly Dictionary<string, string> FaceFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SegoeUI#Regular"] = "segoeui.ttf",
        ["SegoeUI#Bold"] = "segoeuib.ttf",
        ["SegoeUI#Italic"] = "segoeuii.ttf",
        ["SegoeUI#BoldItalic"] = "segoeuiz.ttf",
        ["Consolas#Regular"] = "consola.ttf",
        ["Consolas#Bold"] = "consolab.ttf",
    };

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var family = familyName.Equals(MonoFamily, StringComparison.OrdinalIgnoreCase) ? MonoFamily : SansFamily;
        var faceName = family switch
        {
            MonoFamily => isBold ? "Consolas#Bold" : "Consolas#Regular",
            _ => (isBold, isItalic) switch
            {
                (true, true) => "SegoeUI#BoldItalic",
                (true, false) => "SegoeUI#Bold",
                (false, true) => "SegoeUI#Italic",
                _ => "SegoeUI#Regular"
            }
        };

        // Consolas has no italic file in the base set we ship for; fall back to regular rather than
        // failing the whole report if italic monospace is ever requested.
        if (!FaceFiles.ContainsKey(faceName))
            faceName = family == MonoFamily ? "Consolas#Regular" : "SegoeUI#Regular";

        return new FontResolverInfo(faceName);
    }

    public byte[] GetFont(string faceName)
    {
        if (!FaceFiles.TryGetValue(faceName, out var fileName))
            throw new InvalidOperationException($"Unknown report font face '{faceName}'.");

        var path = Path.Combine(FontsDir, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Expected Windows font file not found: {path}. The report requires Segoe UI and Consolas, " +
                "which ship with Windows by default.", path);

        return File.ReadAllBytes(path);
    }
}
