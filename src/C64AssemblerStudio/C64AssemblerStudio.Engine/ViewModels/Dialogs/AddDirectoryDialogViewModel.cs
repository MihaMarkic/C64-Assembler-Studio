using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;

namespace C64AssemblerStudio.Engine.ViewModels.Dialogs;

public class AddDirectoryDialogViewModel : NotifiableObject, IDialogViewModel<SimpleDialogResult>
{
    public RelayCommand CreateCommand { get; }
    public RelayCommand CancelCommand { get; }
    public string? DirectoryName { get; set; }
    public Action<SimpleDialogResult>? Close { get; set; }
    /// <summary>
    /// This property should be set, otherwise can't create new file
    /// </summary>
    public string? RootDirectory { get; set; } 
    public string? Error { get; private set; }
    public AddDirectoryDialogViewModel()
    {
        CancelCommand = new RelayCommand(Cancel);
        CreateCommand = new RelayCommand(Create, () => !string.IsNullOrWhiteSpace(DirectoryName));
    }
    void Cancel()
    {
        Close?.Invoke(new SimpleDialogResult(DialogResultCode.Cancel));
    }

    void Create()
    {
        string directoryName = Path.Combine(RootDirectory.ValueOrThrow(), DirectoryName.ValueOrThrow());
        try
        {
            Directory.CreateDirectory(directoryName);
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.OK));
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }protected override void OnPropertyChanged(string name = default!)
    {
        switch (name)
        {
            case nameof(DirectoryName):
                Error = null;
                CreateCommand.RaiseCanExecuteChanged();
                break;
        }
        base.OnPropertyChanged(name);
    }
}