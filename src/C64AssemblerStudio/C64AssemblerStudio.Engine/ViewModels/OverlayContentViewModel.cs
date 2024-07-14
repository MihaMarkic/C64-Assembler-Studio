using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public abstract class OverlayContentViewModel : ScopedViewModel
{
    protected readonly IDispatcher _dispatcher;
    public RelayCommand CloseCommand { get; }
    protected OverlayContentViewModel(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        CloseCommand = new(() =>
        {
            Closing();
            dispatcher.Dispatch(new CloseOverlayMessage());
        });
    }
    protected virtual void Closing()
    { }
}