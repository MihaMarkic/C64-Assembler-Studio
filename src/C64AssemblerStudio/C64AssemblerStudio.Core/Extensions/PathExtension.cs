using System.Collections.Immutable;

namespace C64AssemblerStudio.Core.Extensions;

public static partial class PathExtension
{
    /// <summary>
    /// Dissects path to OS independent segments.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>Path segments as a <see cref="ReadOnlySpan<Range>"/></returns>
    public static ReadOnlySpan<Range> PathAsSegmentRanges(this ReadOnlySpan<char> path)
    {
        if (path.Length == 0)
        {
            return [];
        }
        int index = 0;
        int start = 0;
        var builder = ImmutableArray.CreateBuilder<Range>();
        while (index < path.Length)
        {
            if (path[index] is '/' or '\\')
            {
                builder.Add(new Range(start, index));
                start = index + 1;
            }
            index++;
        }
        if (index > start)
        {
            builder.Add(new Range(start, index));
        }
        return builder.ToImmutable().AsSpan();
    }

    public static SegmentedPath PathAsSegmented(this ReadOnlySpan<char> path)
    {
        return new(path, PathAsSegmentRanges(path));
    }

    public static bool PathSegmentsStartWith(this SegmentedPath source, SegmentedPath startsWith, StringComparison comparison)
    {
        if (startsWith.Ranges.Length > source.Ranges.Length)
        {
            return false;
        }
        for (int i = 0; i < startsWith.Ranges.Length; i++)
        {
            {
                var startSegment = startsWith.GetSegment(i);
                var sourceSegment = source.GetSegment(i);
                // for last segment check for startwith as opposed to equals for others
                if (i < startsWith.Ranges.Length - 1 && !MemoryExtensions.Equals(startSegment, sourceSegment, comparison)
                    || !MemoryExtensions.StartsWith(sourceSegment, startSegment, comparison))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static bool PathStartsWithSeparatorAgnostic(this string sourcePath, string startsWithPath, StringComparison comparison)
    {
        var source = sourcePath.AsSpan().PathAsSegmented();
        var startsWith = startsWithPath.AsSpan().PathAsSegmented();

        return source.PathSegmentsStartWith(startsWith, comparison);
    }
}
