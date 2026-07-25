using Avalonia;
using Avalonia.Controls;

namespace ERP.Desktop.Controls;

/// <summary>
/// Təkrar-istifadəli ⓘ info düyməsi — klik edəndə başlıq + izah (+ opsional qeyd) açır.
/// İstifadə: &lt;ctrl:InfoHelp Title="..." Text="..." Note="..." /&gt; (DRY — bütün ekranlarda).
/// </summary>
public partial class InfoHelp : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<InfoHelp, string?>(nameof(Title));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<InfoHelp, string?>(nameof(Text));

    public static readonly StyledProperty<string?> NoteProperty =
        AvaloniaProperty.Register<InfoHelp, string?>(nameof(Note));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Note
    {
        get => GetValue(NoteProperty);
        set => SetValue(NoteProperty, value);
    }

    public InfoHelp() => InitializeComponent();
}
