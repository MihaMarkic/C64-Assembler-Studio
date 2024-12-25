using System.Collections.Frozen;

namespace C64AssemblerStudio.Core.Services.Abstract;

public interface IOsDependent
{
    StringComparison FileStringComparison { get; }
    StringComparer FileStringComparer { get; }
    string ViceExeName { get; }
    string JavaExeName { get; }
    string FileAppOpenName { get; }

    FrozenSet<string> ToFileFrozenSet(IList<string> files) => files.ToFrozenSet(FileStringComparer);
}