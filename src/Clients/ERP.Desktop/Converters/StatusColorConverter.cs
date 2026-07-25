using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ERP.Desktop.Converters;

/// <summary>
/// Sifariş statusunu ona uyğun rəngə çevirir (statusa görə rəngli göstərici üçün).
/// Qaralama=boz, Təsdiqlənmiş=mavi, Təhvil verilmiş=yaşıl, Qaytarılmış=bənövşəyi, Ləğv=qırmızı.
/// </summary>
public sealed class StatusColorConverter : IValueConverter
{
    public static readonly StatusColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = value?.ToString() switch
        {
            "Qaralama" => "#868E96",        // boz — hələ layihə
            "Təsdiqlənmiş" => "#1971C2",    // mavi — təsdiqlənib, rezerv
            "TəhvilVerilmiş" => "#2F9E44",  // yaşıl — müştəridə
            "Qaytarılmış" => "#7048E8",     // bənövşəyi — geri qaytarılıb
            "Ləğv" => "#E03131",            // qırmızı — ləğv
            _ => "#ADB5BD"
        };
        return SolidColorBrush.Parse(hex);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
