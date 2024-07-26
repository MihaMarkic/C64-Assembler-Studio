using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace C64AssemblerStudio.Desktop.Views.Tools;

public partial class Register : UserControl
{
    object? _value;
    string? _caption;
    public static readonly DirectProperty<Register, object?> ValueProperty =
        AvaloniaProperty.RegisterDirect<Register, object?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);
    public static readonly DirectProperty<Register, string?> CaptionProperty =
        AvaloniaProperty.RegisterDirect<Register, string?>(nameof(Caption), o => o.Caption, (o, v) => o.Caption = v);
    public Register()
    {
        InitializeComponent();
    }
    public object? Value
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref this._value, value);
    }
    public string? Caption
    {
        get => _caption;
        set => SetAndRaise(CaptionProperty, ref _caption, value);
    }
}