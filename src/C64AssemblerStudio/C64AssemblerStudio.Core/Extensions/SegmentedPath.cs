namespace C64AssemblerStudio.Core.Extensions;

public static partial class PathExtension
{
    public ref struct SegmentedPath
    {
        public ReadOnlySpan<char> Path { get; }
        public ReadOnlySpan<Range> Ranges { get; }
        public SegmentedPath(ReadOnlySpan<char> path, ReadOnlySpan<Range> ranges)
        {
            Path = path;
            Ranges = ranges;
        }
        public ReadOnlySpan<char> GetSegment(Range r) => Path[r];
        public ReadOnlySpan<char> GetSegment(int rangeIndex) => Path[Ranges[rangeIndex]];
    }
}
