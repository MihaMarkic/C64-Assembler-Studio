using Avalonia.Input;
using AvaloniaEdit.Rendering;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace C64AssemblerStudio.Desktop.Controls.SyntaxEditor;

public class ReferencedFileVisualLineLinkText : VisualLineLinkText
{
    private readonly FileReferenceSyntaxItem _item;
    private Action<FileReferenceSyntaxItem> OnClick { get; set; }

    public ReferencedFileVisualLineLinkText(VisualLine parentVisualLine, int length, 
        FileReferenceSyntaxItem item, Action<FileReferenceSyntaxItem> onClick) :
        base(parentVisualLine, length)
    {
        this._item = item;
        OnClick = onClick;
        NavigateUri = new Uri($"referenced-file://{_item.ReferencedFile.RelativeFilePath}");
        RequireControlModifierForClick = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.Handled && LinkIsClickable(e.KeyModifiers))
        {
            OnClick(_item);
            e.Handled = true;
        }
        base.OnPointerPressed(e);
    }
}