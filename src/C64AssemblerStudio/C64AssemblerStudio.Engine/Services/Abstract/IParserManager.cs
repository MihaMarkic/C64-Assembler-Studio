namespace C64AssemblerStudio.Engine.Services.Abstract;

/// <summary>
/// Coordinates global parsing and reparsing. 
/// </summary>
public interface IParserManager
{
    Task RunInitialParseAsync(CancellationToken ct);
    Task ReparseChangesAsync(CancellationToken ct);
}