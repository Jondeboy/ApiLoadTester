using System.Globalization;
using System.Windows.Data;

namespace ApiLoadTester.App.Converters;

/// <summary>Disables the body template editor for GET/HEAD, which never send a body.</summary>
public sealed class HttpMethodAllowsBodyConverter : IValueConverter
{
    public static readonly HttpMethodAllowsBodyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var method = value as string;
        return !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
