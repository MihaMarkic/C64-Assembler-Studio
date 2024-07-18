using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Messages;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class ErrorMessagesViewModel: NotifiableObject
{
    public ObservableCollection<ErrorMessage> Messages { get; } = new ObservableCollection<ErrorMessage>();
    private readonly ISubscription _messagesSubscription;
    public ErrorMessagesViewModel(IDispatcher dispatcher)
    {
        _messagesSubscription = dispatcher.Subscribe<ErrorMessage>(OnErrorMessage);
    }
    void OnErrorMessage(ErrorMessage message)
    {
        Messages.Add(message);
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