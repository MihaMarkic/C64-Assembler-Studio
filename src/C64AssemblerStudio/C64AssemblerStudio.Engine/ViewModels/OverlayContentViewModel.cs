using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public abstract class OverlayContentViewModel : ScopedViewModel
{
    protected readonly IDispatcher dispatcher;
    public RelayCommand CloseCommand { get; }
    public OverlayContentViewModel(IDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
        CloseCommand = new(() =>
        {
            Closing();
            dispatcher.Dispatch(new CloseOverlayMessage());
        });
    }
    protected virtual void Closing()
    { }
}