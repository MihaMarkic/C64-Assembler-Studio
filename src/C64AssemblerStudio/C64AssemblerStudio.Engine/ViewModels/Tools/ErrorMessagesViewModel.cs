using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class ErrorMessagesViewModel: OutputViewModel<ErrorMessage>
{
    private readonly ISubscription _messagesSubscription;
    public override string Header { get; } = "Error Messages";
    public ErrorMessagesViewModel(IDispatcher dispatcher)
    {
        _messagesSubscription = dispatcher.Subscribe<ErrorMessage>(OnErrorMessage);
    }
    void OnErrorMessage(ErrorMessage message)
    {
        Lines.Add(message);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _messagesSubscription.Dispose();
        }
        base.Dispose(disposing);
    }
}