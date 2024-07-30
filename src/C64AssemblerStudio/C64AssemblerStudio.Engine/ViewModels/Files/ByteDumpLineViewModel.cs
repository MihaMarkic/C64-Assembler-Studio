using C64AssemblerStudio.Core;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

/// <summary>
/// Represents a single bytedump line
/// </summary>
public class ByteDumpLineViewModel : NotifiableObject
{
    public bool IsHighlighted { get; set; }
    private readonly AssemblyLine _assemblyLine;
    public ByteDumpLineViewModel(AssemblyLine assemblyLine)
    {
        _assemblyLine = assemblyLine;
    }

    public ushort Address => _assemblyLine.Address;
    public ImmutableArray<byte> Bytes => _assemblyLine.Data;
    public ImmutableArray<string> Labels => _assemblyLine.Labels;
    public string? Description => _assemblyLine.Description;

}