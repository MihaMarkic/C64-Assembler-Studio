using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public class AssemblerFileViewModel : ProjectFileViewModel
{
    public AssemblerFileViewModel(ILogger<AssemblerFileViewModel> logger, IFileService fileService, Globals globals,
        ProjectFile file) : base(
        logger, fileService, globals, file)
    {
    }
}