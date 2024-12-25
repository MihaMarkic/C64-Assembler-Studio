using System.Runtime.InteropServices;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Core.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace C64AssemblerStudio.Core;

public static class IoC
{
    public static IHost Host { get; private set; } = null!;
    /// <summary>
    /// Has to be called before IoC is used, usually at very program start.
    /// </summary>
    /// <param name="host"></param>
    public static void Init(IHost host)
    {
        Host = host;
    }

    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services
            .AddSingleton<EnumDisplayTextMapper>()
            .AddSingleton<IFileService, FileService>()
            .AddScoped<IServiceCreator, ServiceCreator>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IOsDependent, WindowsDependent>();
        }
        else
        {
            services.AddSingleton<IOsDependent, NonWindowsDependent>();
        }
        return services;
    }
}
