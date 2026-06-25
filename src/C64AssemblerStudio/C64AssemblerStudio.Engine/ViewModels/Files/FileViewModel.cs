using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public abstract class FileViewModel : ScopedViewModel
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public string? ErrorText { get; protected set; }
    protected ILogger<FileViewModel> Logger { get; }
    protected IFileService FileService { get; }
    protected StatusInfoViewModel StatusInfo { get; }
    protected IDispatcher Dispatcher { get; }
    public string? Caption { get; protected set; }
    public bool HasChanges { get; protected set; }
    /// <summary>
    /// Tracks last change time. If <see cref="HasChanges"/> is false, then this value doesn't have a meaning.
    /// </summary>
    public DateTimeOffset LastChangeTime { get; protected set; }
    public RelayCommandAsync SaveCommand { get; }
    /// <summary>
    /// 1 based Caret line position
    /// </summary>
    public int CaretLine { get; set; }
    public int CaretColumn { get; set; }
    /// <summary>
    /// Characters count from begging of content.
    /// </summary>
    public int CaretOffset { get; set; }
    protected FileViewModel(ILogger<FileViewModel> logger, IFileService fileService, IDispatcher dispatcher,
        StatusInfoViewModel statusInfo, IServiceScopeFactory serviceScopeFactory)
    {
        Logger = logger;
        FileService = fileService;
        Dispatcher = dispatcher;
        StatusInfo = statusInfo;
        _serviceScopeFactory = serviceScopeFactory;
        SaveCommand = new RelayCommandAsync(SaveContentAsync, () => HasChanges);
    }
    public async Task SaveContentAsync()
    {
        await SaveContentAsync(CancellationToken.None);
        HasChanges = false;
    }

    protected override void OnPropertyChanged(string? name = null!)
    {
        switch (name)
        {
            case nameof(HasChanges):
                if (HasChanges)
                {
                    LastChangeTime = DateTimeOffset.UtcNow;
                }
                break;
        }
        base.OnPropertyChanged(name);
    }

    protected virtual Task SaveContentAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Handles closing request and giving user option to save when modified.
    /// </summary>
    /// <param name="file"></param>
    /// <returns>True when file can be closed, false otherwise.</returns>
    internal virtual Task<bool> HandlesCloseFileAsync()
    {
        return Task.FromResult(!HasChanges);
    }
    internal async Task<SaveFilesDialogResultCode> ShowDialogForClosingFiles(ImmutableArray<ProjectFile> files, CancellationToken ct = default)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.CreateScopedContent<SaveFileDialogViewModel>();
            detailViewModel.UnsavedFiles = files;
            var dialog = new ShowModalDialogMessage<SaveFileDialogViewModel, SaveFilesDialogResult>(
                "Save files", DialogButton.Save | DialogButton.DoNotSave | DialogButton.Cancel, detailViewModel)
            {
                MinSize = new Size(300, 200),
                DesiredSize = new Size(500, 300),
            };
            Dispatcher.DispatchShowModalDialog(dialog);
            var result = await dialog.Result;
            return result.Code;
        }
    }
}