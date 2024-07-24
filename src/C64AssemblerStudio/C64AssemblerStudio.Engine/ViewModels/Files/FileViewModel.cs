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
    public RelayCommandAsync SaveCommand { get; }
    protected FileViewModel(ILogger<FileViewModel> logger, IFileService fileService, IDispatcher dispatcher,
        StatusInfoViewModel statusInfo)
    {
        Logger = logger;
        FileService = fileService;
        Dispatcher = dispatcher;
        StatusInfo = statusInfo;
        SaveCommand = new RelayCommandAsync(SaveContentAsync, () => HasChanges);
    }
    async Task SaveContentAsync()
    {
        await SaveContentAsync(default);
    }

    public virtual Task SaveContentAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}