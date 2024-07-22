using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public abstract class ProjectFileViewModel : FileViewModel
{
    public ProjectFile File { get; }
    protected Globals Globals { get; }
    public BusyIndicator BusyIndicator { get; } = new();
    public string Content { get; set; }
    public bool IsReadOnly => string.IsNullOrEmpty(Content);

    protected ProjectFileViewModel(ILogger<ProjectFileViewModel> logger, IFileService fileService,
        IDispatcher dispatcher, Globals globals, ProjectFile file) :
        base(logger, fileService, dispatcher)
    {
        File = file;
        Globals = globals;
        Caption = file.Name;
        Content = "";
    }

    public async Task LoadContentAsync(CancellationToken ct = default)
    {
        using (BusyIndicator.Increase())
        {
            string path = Path.Combine(Globals.Project.Directory.ValueOrThrow(), File.GetRelativeDirectory(),
                File.Name);
            try
            {
                Content = await FileService.ReadAllTextAsync(path, ct);
                HasChanges = false;
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
        }
    }

    protected override void OnPropertyChanged(string name = default!)
    {
        switch (name)
        {
            case nameof(Content):
                HasChanges = true;
                break;
            case nameof(HasChanges):
                SaveCommand.RaiseCanExecuteChanged();
                break;
        }

        base.OnPropertyChanged(name);
    }

    public override async Task SaveContentAsync(CancellationToken ct = default)
    {
        using (BusyIndicator.Increase())
        {
            string path = Path.Combine(Globals.Project.Directory.ValueOrThrow(), File.GetRelativeDirectory(),
                File.Name);
            try
            {
                await FileService.WriteAllTextAsync(path, Content, ct);
                HasChanges = false;
            }
            catch (Exception ex)
            {
                Dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Save content", ex.Message));
                ErrorText = ex.Message;
            }
        }
    }
}