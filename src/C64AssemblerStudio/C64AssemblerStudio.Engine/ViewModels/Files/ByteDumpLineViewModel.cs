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
    public bool IsHighlighted { get; set; }
    private readonly ByteDumpLine _byteDumpLine;
    public SourceFileLocation SourceFileLocation { get; } 
    public ByteDumpLineViewModel(ByteDumpLine byteDumpLine)
    {
        _byteDumpLine = byteDumpLine;
        SourceFileLocation = new SourceFileLocation(SourceFile, FileLocation);
    }

    public SourceFile SourceFile => _byteDumpLine.SourceFile;
    public TextRange FileLocation => _byteDumpLine.FileLocation;
    public ushort Address => _byteDumpLine.AssemblyLine.Address;
    public ImmutableArray<byte> Bytes => _byteDumpLine.AssemblyLine.Data;
    public ImmutableArray<string> Labels => _byteDumpLine.AssemblyLine.Labels;
    public string? Description => _byteDumpLine.AssemblyLine.Description;

}