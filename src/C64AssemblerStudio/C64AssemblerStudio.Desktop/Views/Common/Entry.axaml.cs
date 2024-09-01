using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace C64AssemblerStudio.Desktop.Views.Common;

/// <summary>
/// Provides UI feedback for entry fields, such as error border through <see cref="HasErrors"/> property.
/// </summary>
public partial class Entry : UserControl
{
    public static readonly DirectProperty<Entry, bool> HasErrorsProperty = AvaloniaProperty.RegisterDirect<Entry, bool>(nameof(HasErrors), 
        o => o.HasErrors,
        (o, v) => o.HasErrors = v,
        defaultBindingMode: BindingMode.OneWay);
    
    private bool _hasErrors;

    public bool HasErrors
    {
        get => _hasErrors;
        set => SetAndRaise(HasErrorsProperty, ref _hasErrors, value);
    }
    public Entry()
    {
        InitializeComponent();
    }
}