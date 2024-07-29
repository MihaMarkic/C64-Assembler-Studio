using System.Runtime.InteropServices;

namespace C64AssemblerStudio.Core;

public static class OsDependent
{
    public static StringComparison FileStringComparison { get; }= RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.CurrentCultureIgnoreCase
        : StringComparison.CurrentCulture;
    public static StringComparer FileStringComparer { get; }= RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.CurrentCultureIgnoreCase
        : StringComparer.CurrentCulture;
}