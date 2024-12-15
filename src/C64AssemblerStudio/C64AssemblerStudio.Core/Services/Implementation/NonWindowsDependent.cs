using C64AssemblerStudio.Core.Services.Abstract;

namespace C64AssemblerStudio.Core.Services.Implementation;

public class NonWindowsDependent : IOsDependent
{
    public StringComparison FileStringComparison => StringComparison.CurrentCulture;
    public StringComparer FileStringComparer => StringComparer.CurrentCulture;
    public string ViceExeName => "x64sc";
    public string JavaExeName => "java";
    public string FileAppOpenName => "open";
}