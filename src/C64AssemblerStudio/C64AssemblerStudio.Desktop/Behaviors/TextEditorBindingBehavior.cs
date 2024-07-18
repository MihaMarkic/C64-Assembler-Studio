using Avalonia;
using Avalonia.Data;
using AvaloniaEdit;

namespace C64AssemblerStudio.Desktop.Behaviors;

public class TextEditorBindingBehavior: ClassicBehavior<TextEditor>
{
    public static readonly DirectProperty<TextEditorBindingBehavior, string>
        TextProperty =
            AvaloniaProperty.RegisterDirect<TextEditorBindingBehavior, string>(
                nameof(Text),
                editor => editor.Text,
                (editor, s) => editor.Text = s,
                string.Empty, BindingMode.TwoWay);
    
    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        set
        {
            if (AssociatedObject is not null)
            {
                AssociatedObject.TextArea.Document.Text = value;
            }
            SetAndRaise(TextProperty, ref _text, value);
        }
    }

    protected override void Attached()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.TextArea.Document.TextChanged += DocumentOnTextChanged;
        }
        base.Attached();
    }

    protected override void Detached()
    {
        AssociatedObject.ValueOrThrow().TextArea.Document.TextChanged -= DocumentOnTextChanged;
        base.Detached();
    }

    private void DocumentOnTextChanged(object? sender, EventArgs e)
    {
        string text = AssociatedObject.ValueOrThrow().TextArea.Document.Text;
        SetAndRaise(TextProperty, ref _text, text);
    }
}