using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;

namespace C64AssemblerStudio.Engine.ViewModels;

public class AddFileViewModel: NotifiableObject, IDialogViewModel<SimpleDialogResult>
{
    public RelayCommand CreateCommand { get; }
    public RelayCommand CancelCommand { get; }
    /// <summary>
    /// This property should be set, otherwise can't create new file
    /// </summary>
    public string? RootDirectory { get; set; } 
    public string? FileName { get; set; }
    public FileType? FileType { get; set; }
    public string? Error { get; private set; }
    public ImmutableArray<FileType> AvailableFileTypes { get; }
    public Action<SimpleDialogResult>? Close { get; set; }

    public AddFileViewModel()
    {
        CancelCommand = new RelayCommand(Cancel);
        CreateCommand = new RelayCommand(Create, () => !string.IsNullOrWhiteSpace(FileName));
        AvailableFileTypes = ImmutableArray<FileType>.Empty.Add(Common.FileType.Assembler);
        FileType = Common.FileType.Assembler;
    }
    void Cancel()
    {
        Close?.Invoke(new SimpleDialogResult(DialogResultCode.Cancel));
    }

    void Create()
    {
        string fileName = Path.Combine(RootDirectory.ValueOrThrow(), FileName.ValueOrThrow());
        if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)) && FileType is not null)
        {
            fileName += FileType switch
            {
                Common.FileType.Assembler => ".asm",
                _ => "",
            };
        }

        try
        {
            File.Create(fileName).Close();
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
        }
        base.OnPropertyChanged(name);
    }
}