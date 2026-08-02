using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ERP.Desktop.Converters;

/// <summary>bool → qırmızı/boz fırça (xəbərdarlıq göstəriciləri üçün, məs. stok aşılıb).</summary>
public sealed class BoolToRedGrayConverter : IValueConverter
{
    public static readonly BoolToRedGrayConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        SolidColorBrush.Parse(value is true ? "#E03131" : "#868E96");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>bool → Bold/Normal (xəbərdarlıq mətnini qalınlaşdırmaq üçün).</summary>
public sealed class BoolToBoldConverter : IValueConverter
{
    public static readonly BoolToBoldConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? FontWeight.Bold : FontWeight.Normal;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
