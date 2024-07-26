using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace C64AssemblerStudio.Desktop.Views.Tools;

public partial class Flags : UserControl
{
    private byte? _value;

    public static readonly DirectProperty<Flags, byte?> ValueProperty =
        AvaloniaProperty.RegisterDirect<Flags, byte?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);

    public Flags()
    {
        InitializeComponent();
    }

    public byte? Value
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref this._value, value);
    }
}