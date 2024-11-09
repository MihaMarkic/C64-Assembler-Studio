using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public abstract class OverlayContentViewModel : ScopedViewModel
{
    protected readonly IDispatcher Dispatcher;
    public bool IsMaximumSize { get; set; } = true;
    public RelayCommandAsync CloseCommand { get; }
    protected OverlayContentViewModel(IDispatcher dispatcher)
    {
        Dispatcher = dispatcher;
        CloseCommand = new(async () =>
        {
            await ClosingAsync();
            dispatcher.Dispatch(new CloseOverlayMessage());
        }, 
        CanClose);
    }
    protected virtual bool CanClose() => true;
    protected virtual Task ClosingAsync(CancellationToken ct = default) =>Task.CompletedTask;
}