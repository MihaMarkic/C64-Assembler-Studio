using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;

namespace C64AssemblerStudio.Engine.ViewModels.Dialogs;

public class RenameItemDialogViewModel: NotifiableObject, IDialogViewModel<SimpleDialogResult>
{
    public RelayCommand CreateCommand { get; }
    public RelayCommand CancelCommand { get; }
    /// <summary>
    /// This property should be set, otherwise can't rename item
    /// </summary>
    public string? RootDirectory { get; set; } 
    public ProjectItem? Item { get; set; } 
    public string Header => Item is ProjectDirectory ? "directory" : "file";
    public string? FileName { get; set; }
    public string? Error { get; private set; }
    public Action<SimpleDialogResult>? Close { get; set; }

    public RenameItemDialogViewModel()
    {
        CancelCommand = new RelayCommand(Cancel);
        CreateCommand = new RelayCommand(Create, () => !string.IsNullOrWhiteSpace(FileName));
    }
    void Cancel()
    {
        Close?.Invoke(new SimpleDialogResult(DialogResultCode.Cancel));
    }

    void Create()
    {
        string newName = Path.Combine(RootDirectory.ValueOrThrow(), FileName.ValueOrThrow());
        var oldName = Path.Combine(RootDirectory.ValueOrThrow(), Item.ValueOrThrow().Name.ValueOrThrow());
        
        try
        {
            File.Move(oldName, newName);
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.OK));
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    protected override void OnPropertyChanged(string name = default!)
    {
        switch (name)
        {
            case nameof(FileName):
                Error = null;
                CreateCommand.RaiseCanExecuteChanged();
                break;
            case nameof(Item):
                FileName = Item?.Name;
                break;
        }
        base.OnPropertyChanged(name);
    }
}