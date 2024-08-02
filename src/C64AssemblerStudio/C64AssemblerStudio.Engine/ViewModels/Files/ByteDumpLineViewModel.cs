using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Models.Files;
using C64AssemblerStudio.Engine.Models.Projects;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

/// <summary>
/// Represents a single bytedump line
/// </summary>
public class ByteDumpLineViewModel : NotifiableObject
{
    private readonly ByteDumpLine _byteDumpLine;
    public SourceFileLocation SourceFileLocation { get; } 
    public bool IsHighlighted { get; set; }
    public bool IsExecutive { get; set; }
    public bool BelongsToFile { get; }
    public ByteDumpLineViewModel(ByteDumpLine byteDumpLine, bool belongsToFile)
    {
        _byteDumpLine = byteDumpLine;
        SourceFileLocation = new SourceFileLocation(SourceFile, FileLocation);
        BelongsToFile = belongsToFile;
    }

    public ByteDumpLineStatus Status
    {
        get
        {
            ByteDumpLineStatus result = ByteDumpLineStatus.None;
            if (IsHighlighted)
            {
                result |= ByteDumpLineStatus.Highlight;
            }

            if (IsExecutive)
            {
                result |= ByteDumpLineStatus.Executive;
            }
            return result;
        }
    }
    public SourceFile SourceFile => _byteDumpLine.SourceFile;
    public TextRange FileLocation => _byteDumpLine.FileLocation;
    public ushort Address => _byteDumpLine.AssemblyLine.Address;
    public ImmutableArray<byte> Bytes => _byteDumpLine.AssemblyLine.Data;
    public ImmutableArray<string> Labels => _byteDumpLine.AssemblyLine.Labels;
    public string? Description => _byteDumpLine.AssemblyLine.Description;
}
[Flags]
public enum ByteDumpLineStatus
{
    None = 0,
    Highlight = 1,
    Executive = 2,
}