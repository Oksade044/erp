using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Reports;

namespace ERP.Desktop.ViewModels;

/// <summary>
/// İcarə təqvimi — verilmiş dövrdə planlaşdırılmış icarələr (təhvil/qaytarma).
/// Planlaşdırma alətidir: hansı gün nə çıxır/qayıdır görünür; sətrə iki klik → sifariş detalı.
/// </summary>
public partial class RentalCalendarViewModel : ViewModelBase
{
    private readonly ErpApiClient _api;

    public RentalCalendarViewModel(ErpApiClient api)
    {
        _api = api;
        var now = DateTime.Now;
        _from = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, DateTimeOffset.Now.Offset);
        _to = _from.AddMonths(1).AddDays(-1);
    }

    public ObservableCollection<RentalCalendarEntryDto> Entries { get; } = [];
    [ObservableProperty] private RentalCalendarEntryDto? _selected;
    [ObservableProperty] private DateTimeOffset _from;
    [ObservableProperty] private DateTimeOffset _to;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    [ObservableProperty] private int _deliveriesInRange;
    [ObservableProperty] private int _returnsInRange;
    [ObservableProperty] private decimal _rangeTotal;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var from = DateOnly.FromDateTime(From.DateTime);
            var to = DateOnly.FromDateTime(To.DateTime);
            if (to < from) { Status = "Bitmə tarixi başlanğıcdan əvvəl ola bilməz."; return; }

            var rows = await _api.GetRentalCalendarAsync(from, to);
            Entries.Clear();
            if (rows is not null) foreach (var r in rows) Entries.Add(r);

            DeliveriesInRange = Entries.Count(e => e.DeliversInRange);
            ReturnsInRange = Entries.Count(e => e.ReturnsInRange);
            RangeTotal = Entries.Sum(e => e.Total);
            Status = Entries.Count == 0
                ? "Bu dövrdə planlaşdırılmış icarə yoxdur."
                : $"{Entries.Count} icarə — sətrə iki dəfə klik edib detala baxın.";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    partial void OnFromChanged(DateTimeOffset value) => LoadCommand.Execute(null);
    partial void OnToChanged(DateTimeOffset value) => LoadCommand.Execute(null);

    /// <summary>Seçilmiş təqvim sətrinin sifariş detal VM-i (kod-arxasından çağırılır).</summary>
    public async Task<OrderDetailViewModel?> CreateOrderDetailAsync()
    {
        if (Selected is null) return null;
        var result = await _api.GetOrdersAsync(Selected.OrderNumber);
        var order = result?.Items.FirstOrDefault(o => o.OrderNumber == Selected.OrderNumber);
        return order is null ? null : new OrderDetailViewModel(_api, order);
    }
}
