using C64AssemblerStudio.Engine.Services.Abstract;

namespace C64AssemblerStudio.Desktop.Services.Implementation;

public class SystemInfo: ISystemInfo
{
    public Version Version => typeof(SystemInfo).Assembly.GetName().Version!;
}