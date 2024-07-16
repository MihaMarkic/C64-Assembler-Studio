using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Engine.Messages;

public abstract record ShowModalDialogMessageCore
{
    public string Caption { get; }
    public Size MinSize { get; init; } = new Size(400, 200);
    public Size DesiredSize { get; init; } = new Size(400, 200);
    public DialogButton Buttons { get; }
    public event EventHandler? Close;
    public NotifiableObject ViewModel { get; }

    protected ShowModalDialogMessageCore(string caption, DialogButton buttons, NotifiableObject viewModel)
    {
        Caption = caption;
        Buttons = buttons;
        ViewModel = viewModel;
    }
    protected void OnClose(EventArgs e)
    {
        Close?.Invoke(this, e);
    }
}
public record ShowModalDialogMessage<TViewModel, TResult> : ShowModalDialogMessageCore
    where TViewModel: NotifiableObject, IDialogViewModel<TResult>
{
    private readonly TaskCompletionSource<TResult> _tcs = new TaskCompletionSource<TResult>();
    public Task<TResult> Result => _tcs.Task;
    public new TViewModel ViewModel => (TViewModel)base.ViewModel;
    public ShowModalDialogMessage(string caption, DialogButton buttons, TViewModel viewModel) :
        base(caption, buttons, viewModel)
    {
        ViewModel.Close = r =>
        {
            OnClose(EventArgs.Empty);
            _tcs.SetResult(r);
        };
    }
}

public record SimpleDialogResult
{
    public DialogResultCode Code { get; init; }
    public SimpleDialogResult(DialogResultCode code)
    {
        Code = code;
    }
}
