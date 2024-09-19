using System.Collections.Frozen;

namespace C64AssemblerStudio.Engine;

public static class C64Globals
{
    public static FrozenSet<string> MemspacePrefixes { get; } =
        FrozenSet.ToFrozenSet(["c", "8", "9", "10", "11"], StringComparer.OrdinalIgnoreCase);
    public static FrozenSet<string> Registers { get; } =
        FrozenSet.ToFrozenSet(["A", "X", "Y", "SP", "PC"], StringComparer.OrdinalIgnoreCase);
}