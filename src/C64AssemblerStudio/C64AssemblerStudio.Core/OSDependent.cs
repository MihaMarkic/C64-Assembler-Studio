using System.Runtime.InteropServices;

namespace C64AssemblerStudio.Core;

public static class OSDependent
{
    public static StringComparison FileStringComparison { get; }= RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.CurrentCultureIgnoreCase
        : StringComparison.CurrentCulture;
}