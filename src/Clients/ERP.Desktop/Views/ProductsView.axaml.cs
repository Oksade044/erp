using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class ProductsView : UserControl
{
    public ProductsView() => InitializeComponent();

    /// <summary>Fayl seçici açır və seçilmiş şəkli məhsula yükləyir (TDD §24).</summary>
    private async void OnUploadImageClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductsViewModel vm) return;
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;

            var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Məhsul şəkli seçin",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Şəkillər") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif"] }]
            });

            var file = files.FirstOrDefault();
            if (file is null) return;

            var path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                await vm.UploadImageAsync(path);
        }
        catch (System.Exception ex)
        {
            // async void — tutulmayan exception bütün proqramı çökdürər. Statusda göstər.
            vm.Status = $"Xəta: {ex.Message}";
        }
    }

    /// <summary>Məhsula iki dəfə klik → tarixçə pəncərəsi (#38).</summary>
    private void OnProductDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductsViewModel vm) return;
        var detail = vm.CreateHistory();
        if (detail is null) return;
        var window = new ProductHistoryWindow { DataContext = detail };
        if (TopLevel.GetTopLevel(this) is Window owner) window.Show(owner);
        else window.Show();
    }

    /// <summary>Yeni məhsul formasında şəkil seçir (məhsul yaradılanda avtomatik yüklənəcək).</summary>
    private async void OnPickNewImageClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductsViewModel vm) return;
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;

            var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Məhsul şəkli seçin",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Şəkillər") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif"] }]
            });

            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                vm.NewImagePath = path;
        }
        catch (System.Exception ex)
        {
            vm.Status = $"Xəta: {ex.Message}";
        }
    }
}
