using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ERP.Desktop;

/// <summary>
/// DataGrid sütun enlərini yadda saxlayan əlavə davranış (#6). İstifadə:
/// <c>local:GridColumnPersistence.Key="products"</c>. İstifadəçi sütunu sola-sağa sürüşdürdükdə
/// (resize) enlər fayla yazılır və növbəti açılışda bərpa olunur. Sütunlar həmçinin resizable edilir.
/// </summary>
public static class GridColumnPersistence
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ERP.Desktop", "grid-widths.json");

    private static Dictionary<string, double[]> _store = Load();

    public static readonly AttachedProperty<string?> KeyProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, string?>("Key", typeof(GridColumnPersistence));

    public static void SetKey(DataGrid grid, string? value) => grid.SetValue(KeyProperty, value);
    public static string? GetKey(DataGrid grid) => grid.GetValue(KeyProperty);

    static GridColumnPersistence()
    {
        KeyProperty.Changed.AddClassHandler<DataGrid>((grid, e) =>
        {
            if (e.NewValue is not string key || string.IsNullOrWhiteSpace(key)) return;
            grid.CanUserResizeColumns = true;

            grid.AttachedToVisualTree += (_, _) => Restore(grid, key);
            // Resize sürükləməsi bitəndə (siçan buraxılanda) enləri saxla.
            grid.AddHandler(InputElement.PointerReleasedEvent,
                (object? _, PointerReleasedEventArgs _) => Save(grid, key),
                Avalonia.Interactivity.RoutingStrategies.Bubble | Avalonia.Interactivity.RoutingStrategies.Tunnel);
        });
    }

    private static void Restore(DataGrid grid, string key)
    {
        if (!_store.TryGetValue(key, out var widths)) return;
        for (int i = 0; i < grid.Columns.Count && i < widths.Length; i++)
            if (widths[i] > 20)
                grid.Columns[i].Width = new DataGridLength(widths[i], DataGridLengthUnitType.Pixel);
    }

    private static void Save(DataGrid grid, string key)
    {
        try
        {
            var widths = grid.Columns.Select(c => c.ActualWidth).ToArray();
            if (widths.Length == 0 || widths.All(w => w <= 0)) return;
            _store[key] = widths;
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            File.WriteAllText(StorePath, JsonSerializer.Serialize(_store));
        }
        catch { /* yaddaş uğursuz olsa da işi dayandırma */ }
    }

    private static Dictionary<string, double[]> Load()
    {
        try
        {
            return File.Exists(StorePath)
                ? JsonSerializer.Deserialize<Dictionary<string, double[]>>(File.ReadAllText(StorePath)) ?? new()
                : new();
        }
        catch { return new(); }
    }
}
