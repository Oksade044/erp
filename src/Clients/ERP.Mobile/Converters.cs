using System.Globalization;

namespace ERP.Mobile;

/// <summary>bool → tərs bool (məs. IsBusy=true → düymə deaktiv).</summary>
public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool b && !b;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool b && !b;
}

/// <summary>Dəyər null/boş deyilsə true (görünürlük üçün).</summary>
public sealed class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s ? !string.IsNullOrWhiteSpace(s) : value is not null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
