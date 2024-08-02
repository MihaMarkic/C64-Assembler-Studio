using System.Runtime.InteropServices;

namespace C64AssemblerStudio.Core;

public static class OsDependent
{
    public static StringComparison FileStringComparison { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.CurrentCultureIgnoreCase
        : StringComparison.CurrentCulture;

    public static StringComparer FileStringComparer { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.CurrentCultureIgnoreCase
        : StringComparer.CurrentCulture;

    public static string ViceExeName { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "x64sc.exe"
        : "x64sc";
    public static string JavaExeName { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "java.exe"
        : "java";
    public static string FileAppOpenName { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "explorer.exe"
        : "open";
}