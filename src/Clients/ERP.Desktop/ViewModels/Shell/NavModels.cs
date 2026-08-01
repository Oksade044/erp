using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ERP.Desktop.ViewModels;

/// <summary>Sol tree-menyuda bir bölmə (klik → sağ iş sahəsində tab açır).</summary>
public sealed class NavItem
{
    public string Label { get; }
    public IRelayCommand Command { get; }
    public bool IsVisible { get; }

    public NavItem(string label, IRelayCommand command, bool isVisible = true)
    {
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

    /// <summary>Ən azı bir görünən alt bölmə varmı (boş qruplar gizlədilir).</summary>
    public bool HasVisible => Items.Any(i => i.IsVisible);
}

/// <summary>Sağ iş sahəsində açıq tab (başlıq + məzmun VM-i + bağla).</summary>
public sealed class WorkspaceTab
{
    public string Key { get; }
    public string Title { get; }
    public ViewModelBase Content { get; }
    public IRelayCommand CloseCommand { get; }

    public WorkspaceTab(string key, string title, ViewModelBase content, IRelayCommand closeCommand)
    {
        Key = key;
        Title = title;
        Content = content;
        CloseCommand = closeCommand;
    }
}
