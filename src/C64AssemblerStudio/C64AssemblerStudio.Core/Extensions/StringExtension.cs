using System.Runtime.InteropServices;

namespace C64AssemblerStudio.Core.Extensions;

public static class StringExtension
{
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