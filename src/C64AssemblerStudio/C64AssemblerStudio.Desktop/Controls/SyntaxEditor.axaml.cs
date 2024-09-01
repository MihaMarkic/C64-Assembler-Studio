using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Media;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;

namespace C64AssemblerStudio.Desktop.Controls;

public partial class SyntaxEditor : UserControl
{
    public static readonly DirectProperty<SyntaxEditor, string?> TextProperty =
        AvaloniaProperty.RegisterDirect<SyntaxEditor, string?>(nameof(Text),
            o => o.Text,
            (o, v) => o.Text = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<SyntaxEditor, ImmutableArray<SyntaxEditorToken>?> TokensProperty =
        AvaloniaProperty.RegisterDirect<SyntaxEditor, ImmutableArray<SyntaxEditorToken>?>(nameof(Tokens),
            o => o.Tokens,
            (o, v) => o.Tokens = v,
            defaultBindingMode: BindingMode.OneWay);
    public static readonly DirectProperty<SyntaxEditor, ImmutableArray<SyntaxEditorError>?> ErrorsProperty =
        AvaloniaProperty.RegisterDirect<SyntaxEditor, ImmutableArray<SyntaxEditorError>?>(nameof(Errors),
            o => o.Errors,
            (o, v) => o.Errors = v,
            defaultBindingMode: BindingMode.OneWay);
    
    private string? _text;
    private readonly ObservableCollection<SyntaxEditorFormating> _formatters = new();
    private ImmutableArray<SyntaxEditorToken>? _tokens;
    private ImmutableArray<SyntaxEditorError>? _errors;
    private readonly SyntaxEditorColorizer _colorizer;
    public SyntaxEditor()
    {
        InitializeComponent();
        _colorizer = new();
        Editor.TextArea.TextView.LineTransformers.Add(_colorizer);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _colorizer.Formatters = Formatters.Where(f => f.Key is not null).ToDictionary(v => v.Key!);
        base.OnAttachedToVisualTree(e);
    }

    public ObservableCollection<SyntaxEditorFormating> Formatters => _formatters; 
    public string? Text
    {
        get => _text;
        set => SetAndRaise(TextProperty, ref _text, value);
    }
    public ImmutableArray<SyntaxEditorToken>? Tokens
    {
        get => _tokens;
        set => SetAndRaise(TokensProperty, ref _tokens, value);
    }
    public ImmutableArray<SyntaxEditorError>? Errors
    {
        get => _errors;
        set => SetAndRaise(ErrorsProperty, ref _errors, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        switch (change.Property.Name)
        {
            case nameof(Tokens):
                {
                    if (change.NewValue is ImmutableArray<SyntaxEditorToken> newValue)
                    {
                        _colorizer.Tokens = newValue;
                    }
                    else
                    {
                        _colorizer.Tokens = [];
                    }
                    Editor.TextArea.TextView.Redraw();
                }
                break;
            case nameof(Errors):
            {
                if (change.NewValue is ImmutableArray<SyntaxEditorError> newValue)
                {
                    _colorizer.Errors = newValue;
                }
                else
                {
                    _colorizer.Errors = [];
                }
                Editor.TextArea.TextView.Redraw();
            }
                break;
        }
        base.OnPropertyChanged(change);
    }

    private void NewValueOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Editor.TextArea.TextView.Redraw();
    }
}

public class SyntaxEditorFormating
{
    public object? Key { get; set; }
    public IBrush? ForegroundColor { get; set; }
    public IBrush? BackgroundColor { get; set; }
}
