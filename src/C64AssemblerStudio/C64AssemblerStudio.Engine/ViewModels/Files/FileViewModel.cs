using C64AssemblerStudio.Core.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public abstract class FileViewModel : ScopedViewModel
{
    public string? ErrorText { get; protected set; }
    protected ILogger<FileViewModel> Logger { get; }
    protected IFileService FileService { get; }
    public string? Caption { get; protected set; }

    protected FileViewModel(ILogger<FileViewModel> logger, IFileService fileService)
    {
        Logger = logger;
        FileService = fileService;
    }
}