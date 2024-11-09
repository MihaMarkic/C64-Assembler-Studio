using Avalonia;
using Avalonia.Controls;
using C64AssemblerStudio.Engine.Messages;

namespace C64AssemblerStudio.Desktop.Views.Dialogs;

public partial class ModalDialogWindow : Window
{
    private ShowModalDialogMessageCore? _message;
    public ModalDialogWindow()
    {
        InitializeComponent();
#if !RELEASE
        this.AttachDevTools();
#endif
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_message is not null)
        {
            _message.Close -= Message_Close;
        }
        _message = DataContext as ShowModalDialogMessageCore;
        if (_message is not null)
        {
            _message.Close += Message_Close;
        }
        base.OnDataContextChanged(e);
    }

    private void Message_Close(object? sender, EventArgs e)
    {
        Close();
    } 
}
