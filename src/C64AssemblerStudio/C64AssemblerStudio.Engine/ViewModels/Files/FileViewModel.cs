using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public abstract class FileViewModel : ScopedViewModel
{
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
        StatusInfoViewModel statusInfo)
    {
        Logger = logger;
        FileService = fileService;
        Dispatcher = dispatcher;
        StatusInfo = statusInfo;
        SaveCommand = new RelayCommandAsync(SaveContentAsync, () => HasChanges);
    }
    public async Task SaveContentAsync()
    {
        await SaveContentAsync(default);
        HasChanges = false;
    }

    protected override void OnPropertyChanged(string name = default!)
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
}