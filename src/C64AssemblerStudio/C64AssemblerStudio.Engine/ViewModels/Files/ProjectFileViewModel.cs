using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public abstract class ProjectFileViewModel : FileViewModel
{
    public ProjectFile File { get; }
    protected Globals Globals { get; }
    public BusyIndicator BusyIndicator { get; } = new();
    public string Content { get; set; } = string.Empty;
    public bool IsReadOnly => string.IsNullOrEmpty(Content);
    /// <summary>
    /// When set defines execution line in paused state
    /// </summary>
    public (int Start, int End)? ExecutionLineRange { get; set; }
    public ushort? ExecutionAddress { get; set; }
    public event EventHandler<MoveCaretEventArgs>? MoveCaretRequest;
    protected bool IsContentLoaded { get; private set; }

    protected ProjectFileViewModel(ILogger<ProjectFileViewModel> logger, IFileService fileService,
        IDispatcher dispatcher, StatusInfoViewModel statusInfo, Globals globals, ProjectFile file,
        IServiceScopeFactory serviceScopeFactory) :
        base(logger, fileService, dispatcher, statusInfo, serviceScopeFactory)
    {
        File = file;
        Globals = globals;
        Caption = file.Name;
    }

    void RaiseMoveCaret(MoveCaretEventArgs e) => MoveCaretRequest?.Invoke(this, e);

    public async Task LoadContentAsync(CancellationToken ct = default)
    {
        using (BusyIndicator.Increase())
        {
            string path = Path.Combine(Globals.Project.Directory.ValueOrThrow(), File.RelativeDirectory,
                File.Name);
            try
            {
                Content = await FileService.ReadAllTextAsync(path, ct);
                HasChanges = false;
                IsContentLoaded = true;
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
        }
    }

    public void MoveCaret(int row, int col)
    {
        RaiseMoveCaret(new MoveCaretEventArgs(row, col));
    }

    protected override void OnPropertyChanged(string name = default!)
    {
        switch (name)
        {
            case nameof(Content):
                LastChangeTime = DateTimeOffset.Now;
                HasChanges = true;
                StatusInfo.BuildingStatus = BuildStatus.Idle;
                break;
            case nameof(HasChanges):
                SaveCommand.RaiseCanExecuteChanged();
                break;
        }

        base.OnPropertyChanged(name);
    }

    protected override async Task SaveContentAsync(CancellationToken ct)
    {
        using (BusyIndicator.Increase())
        {
            string path = Path.Combine(Globals.Project.Directory.ValueOrThrow(), File.RelativeDirectory,
                File.Name);
            try
            {
                await FileService.WriteAllTextAsync(path, Content, ct);
                HasChanges = false;
            }
            catch (Exception ex)
            {
                await Dispatcher.DispatchAsync(new ErrorMessage(ErrorMessageLevel.Error, "Save content", ex.Message), ct: ct);
                ErrorText = ex.Message;
            }
        }
    }
    /// <inheritdoc />
    internal override async Task<bool> HandlesCloseFileAsync()
    {
        if (HasChanges)
        {
            var resultCode = await ShowDialogForClosingFiles([File], CancellationToken.None);
            switch (resultCode)
            {
                case SaveFilesDialogResultCode.Save:
                    try
                    {
                        await SaveContentAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to save {File}", File.Name);
                    }
                    break;
                case SaveFilesDialogResultCode.DoNotSave:
                    break;
                default:
                    return false;
            }
        }

        return true;
    }
}