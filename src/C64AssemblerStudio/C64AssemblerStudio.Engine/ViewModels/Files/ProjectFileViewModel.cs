using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public abstract class ProjectFileViewModel : FileViewModel
{
    protected ProjectFile File { get; }
    protected Globals Globals { get; }
    public BusyIndicator BusyIndicator { get; } = new();
    public string? Content { get; set; }
    public bool IsReadOnly => Content is not null;
    protected ProjectFileViewModel(ILogger<ProjectFileViewModel> logger, IFileService fileService, Globals globals, ProjectFile file) :
        base(logger, fileService)
    {
        File = file;
        Globals = globals;
        Caption = file.Name;
    }

    public async Task LoadContentAsync(CancellationToken ct = default)
    {
        using (BusyIndicator.Increase())
        {
            string path = Path.Combine(Globals.Project.Directory.ValueOrThrow(), File.GetRelativeDirectory(), File.Name);
            try
            {
                Content = await System.IO.File.ReadAllTextAsync(path, ct);
            }
            catch (Exception ex)
            {
                ErrorText = ex.Message;
            }
        }
    }
}