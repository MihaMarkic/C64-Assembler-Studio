using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.ViewModels.Dialogs;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public class SaveFileDialogViewModel: ScopedViewModel, IDialogViewModel<SaveFilesDialogResult>
{
    public RelayCommand SaveCommand { get; }
    public RelayCommand DoNotSaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public Action<SaveFilesDialogResult>? Close { get; set; }
    public ImmutableArray<ProjectFile> UnsavedFiles { get; set; } = ImmutableArray<ProjectFile>.Empty;

    public SaveFileDialogViewModel()
    {
        SaveCommand = new RelayCommand(() => Close?.Invoke(new SaveFilesDialogResult(SaveFilesDialogResultCode.Save)));
        DoNotSaveCommand = new RelayCommand(() => Close?.Invoke(new SaveFilesDialogResult(SaveFilesDialogResultCode.DoNotSave)));
        CancelCommand = new RelayCommand(() => Close?.Invoke(new SaveFilesDialogResult(SaveFilesDialogResultCode.Cancel)));
    }
}

public record SaveFilesDialogResult
{
    public SaveFilesDialogResultCode Code { get; init; }
    public SaveFilesDialogResult(SaveFilesDialogResultCode code)
    {
        Code = code;
    }
}

public enum SaveFilesDialogResultCode
{
    Save,
    DoNotSave,
    Cancel
}