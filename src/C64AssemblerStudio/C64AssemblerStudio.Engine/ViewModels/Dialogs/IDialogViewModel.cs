namespace C64AssemblerStudio.Engine.ViewModels.Dialogs;

public interface IDialogViewModel<TResult>
{
    Action<TResult>? Close { get; set; }
}
