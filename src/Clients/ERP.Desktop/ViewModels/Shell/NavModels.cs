using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ERP.Desktop.ViewModels;

/// <summary>Sol tree-menyuda bir bölmə (ikon + ad). Klik → sağ iş sahəsində tab açır.</summary>
public sealed partial class NavItem : ObservableObject
{
    public string Key { get; }
    public string Icon { get; }
    public string Label { get; }
    public IRelayCommand Command { get; }
    public bool IsVisible { get; }

    /// <summary>Hazırda açıq/seçili bölmədirsə — vurğulanır.</summary>
    [ObservableProperty] private bool _isActive;

    public NavItem(string key, string icon, string label, IRelayCommand command, bool isVisible = true)
    {
        Key = key;
        Icon = icon;
        Label = label;
        Command = command;
        IsVisible = isVisible;
    }
}

/// <summary>Tree-menyuda qrup (genişlənən başlıq + alt bölmələr).</summary>
public sealed partial class NavGroup : ObservableObject
{
    public string Title { get; }
    public ObservableCollection<NavItem> Items { get; }
    [ObservableProperty] private bool _isExpanded;

    public NavGroup(string title, IEnumerable<NavItem> items, bool isExpanded = false)
    {
        Title = title;
        Items = new ObservableCollection<NavItem>(items);
        _isExpanded = isExpanded;
    }

    public bool HasVisible => Items.Any(i => i.IsVisible);
}

/// <summary>Sağ iş sahəsində açıq tab (ikon + başlıq + məzmun VM-i + bağla).</summary>
public sealed class WorkspaceTab
{
    public string Key { get; }
    public string Icon { get; }
    public string Title { get; }
    public ViewModelBase Content { get; }
    public IRelayCommand CloseCommand { get; }

    public WorkspaceTab(string key, string icon, string title, ViewModelBase content, IRelayCommand closeCommand)
    {
        Key = key;
        Icon = icon;
        Title = title;
        Content = content;
        CloseCommand = closeCommand;
    }
}
