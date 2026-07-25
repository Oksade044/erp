using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ERP.Desktop.Converters;

/// <summary>
/// Məhsulun izləmə rejiminin daxili adını (Toplu/Nüsxə) istifadəçi üçün aydın mətnə çevirir.
/// Toplu → "Say ilə idarə olunur", Nüsxə → "Fərdi izlənilir (Serial/QR)".
/// </summary>
public sealed class TrackingModeConverter : IValueConverter
{
    public static readonly TrackingModeConverter Instance = new();

    public const string BulkDisplay = "Say ilə idarə olunur";
    public const string IndividualDisplay = "Fərdi izlənilir (Serial/QR)";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() == "Nüsxə" ? IndividualDisplay : BulkDisplay;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() == IndividualDisplay ? "Nüsxə" : "Toplu";
}
