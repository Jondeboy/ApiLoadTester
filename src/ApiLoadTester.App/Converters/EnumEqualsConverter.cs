using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ApiLoadTester.App.Converters;

/// <summary>Binds a group of RadioButtons to a single enum-valued property: each RadioButton's
/// IsChecked compares the bound enum against its own ConverterParameter, and checking one writes
/// that parameter's value back.</summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public static readonly EnumEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && value.Equals(parameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? parameter! : Binding.DoNothing;
}

/// <summary>Shows/collapses a panel based on whether an enum-valued property equals the ConverterParameter.</summary>
public sealed class EnumVisibilityConverter : IValueConverter
{
    public static readonly EnumVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && value.Equals(parameter) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
