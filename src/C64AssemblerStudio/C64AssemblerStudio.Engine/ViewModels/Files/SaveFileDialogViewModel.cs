using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.ViewModels.Dialogs;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public class SaveFileDialogViewModel: ScopedViewModel, IDialogViewModel<SaveFilesDialogResult>
{
    public record UnsavedFile(string? Directory, string Name);
    public RelayCommand SaveCommand { get; }
    public RelayCommand DoNotSaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public Action<SaveFilesDialogResult>? Close { get; set; }
    public ImmutableArray<ProjectFile> UnsavedFiles { get; set; } = ImmutableArray<ProjectFile>.Empty;

    public IEnumerable<UnsavedFile> UnsavedFileItems =>
        UnsavedFiles.Select(f => new UnsavedFile(f.RelativeDirectory, f.Name));
    public SaveFileDialogViewModel()
    {
        SaveCommand = new RelayCommand(() => Close?.Invoke(new SaveFilesDialogResult(SaveFilesDialogResultCode.Save)));
        DoNotSaveCommand = new RelayCommand(() => Close?.Invoke(new SaveFilesDialogResult(SaveFilesDialogResultCode.DoNotSave)));
        CancelCommand = new RelayCommand(() => Close?.Invoke(new SaveFilesDialogResult(SaveFilesDialogResultCode.Cancel)));
    }
}

public class DesignSaveFileDialogViewModel : SaveFileDialogViewModel
{
    public DesignSaveFileDialogViewModel()
    {
        UnsavedFiles =
        [
            new ProjectFile(StringComparison.Ordinal)
                { FileType = FileType.Assembler, Name = "misc.asm", Parent = null },
            new ProjectFile(StringComparison.Ordinal)
            {
                FileType = FileType.Assembler, Name = "relative.asm",
                Parent = new ProjectDirectory(StringComparison.Ordinal) { Name = "subdir", Parent = null }
            }
        ];
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