using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public interface ISourcecodeParser
{
    /// <summary>
    /// Trigger reparsing of source code
    /// </summary>
    /// <param name="changedFile"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task ParseAsync(ProjectFile? changedFile, CancellationToken ct = default);
}
public class SourceCodeParser: ISourcecodeParser
{
    private readonly Globals _globals;
    private CancellationTokenSource? _parsingCts;

    public Task ParseAsync(ProjectFile? changedFile, CancellationToken ct)
    {
        _parsingCts?.Cancel();
        _parsingCts = new();

        throw new NotImplementedException();
    }
}