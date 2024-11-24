using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;

namespace C64AssemblerStudio.Core.Extensions;

public static class StringExtension
{
    /// <summary>
    /// Converts given string with tabs to string with spaces.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="tabWidth"></param>
    /// <returns></returns>
    /// <threadsafety>Method is thread safe.</threadsafety>
    public static string? ConvertTabsToSpaces(this string? source, int tabWidth)
{
        if (!string.IsNullOrWhiteSpace(source) && source.Contains('\t'))
        {
            var builder = new StringBuilder(1024);
            // 20 is magic number for additional space based on tabs
            foreach (char c in source)
            {
                if (c != '\t')
                {
                    builder.Append(c);
                }
                else
                {
                    int spacesCount = CalculateRequiredSpacesForTab(builder.Length, tabWidth);
                    builder.Append(' ', spacesCount);
                }
            }
            return builder.ToString();
        }
        else
        {
            return source;
        }
    }

    internal static int CalculateRequiredSpacesForTab(int position, int tabWidth)
    {
        return tabWidth - position % tabWidth;
    }

    public static ImmutableArray<string> AsSplitStringArray(this string? source, char separator = ';')
    {
        return source is not null ? [..source.Split(separator)] : ImmutableArray<string>.Empty;
    }

    public static ReadOnlySpan<char> ExtractLine(this ReadOnlySpan<char> text, int lineNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineNumber);

        var currentText = text;
        int lineEndIndex = text.IndexOf(Environment.NewLine);
        int current = 0;
        while (current < lineNumber)
        {
            if (lineEndIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber));
            }
            currentText = currentText[(lineEndIndex + Environment.NewLine.Length)..]; 
            lineEndIndex = currentText.IndexOf(Environment.NewLine);
            current++;
        }

        if (lineEndIndex < 0)
        {
            return currentText;
        }
        else
        {
            return currentText[..lineEndIndex];
        }
        
    }
    public static string ConvertsDirectorySeparators(this string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }
        else
        {
            return path.Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}