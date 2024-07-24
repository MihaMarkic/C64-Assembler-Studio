using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace C64AssemblerStudio.Desktop.Views.Common;

public partial class Spinner : UserControl
{
    public static readonly DirectProperty<Spinner, bool> IsActiveProperty = AvaloniaProperty.RegisterDirect<Spinner, bool>(nameof(IsActive),
        o => o.IsActive, (o ,v) => o.IsActive = v, defaultBindingMode: BindingMode.OneWay);
    bool _isActive;
    public Spinner()
    {
        InitializeComponent();
    }
    public bool IsActive
    {
        get => _isActive;
        set => SetAndRaise(IsActiveProperty, ref _isActive, value);
    }
}