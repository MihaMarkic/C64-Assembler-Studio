using C64AssemblerStudio.Core.Services.Abstract;

namespace C64AssemblerStudio.Core.Services.Implementation;

public class WindowsDependent : IOsDependent
{
    public StringComparison FileStringComparison => StringComparison.CurrentCultureIgnoreCase;
    public StringComparer FileStringComparer => StringComparer.CurrentCultureIgnoreCase;
    public string ViceExeName => "x64sc.exe";
    public string JavaExeName => "java.exe";
    public string FileAppOpenName => "explorer.exe";
    public string NormalizePath(string path) => path.Replace('/', '\\');
}