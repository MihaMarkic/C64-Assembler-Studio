using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public abstract class FileViewModel : ScopedViewModel
{
    public string? ErrorText { get; protected set; }
    protected ILogger<FileViewModel> Logger { get; }
    protected IFileService FileService { get; }
    public string? Caption { get; protected set; }
    public bool HasChanges { get; protected set; }
    public RelayCommandAsync SaveCommand { get; }
    protected FileViewModel(ILogger<FileViewModel> logger, IFileService fileService)
    {
        Logger = logger;
        FileService = fileService;
        SaveCommand = new RelayCommandAsync(SaveContentAsync, () => HasChanges);
    }
    async Task SaveContentAsync()
    {
        await SaveContentAsync(default);
    }

    protected virtual Task SaveContentAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}