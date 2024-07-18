using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public class AssemblerFileViewModel : ProjectFileViewModel
{
    public AssemblerFileViewModel(ILogger<AssemblerFileViewModel> logger, IFileService fileService, IDispatcher dispatcher, Globals globals,
        ProjectFile file) : base(
        logger, fileService, dispatcher, globals, file)
    {
    }
}