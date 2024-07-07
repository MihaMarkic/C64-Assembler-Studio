namespace C64AssemblerStudio.Engine.ViewModels;

public interface IDialogViewModel<TResult>
{
    Action<TResult>? Close { get; set; }
}
