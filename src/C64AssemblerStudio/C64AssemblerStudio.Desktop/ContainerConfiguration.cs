using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Desktop.Services.Implementation;
using C64AssemblerStudio.Desktop.Views;
using C64AssemblerStudio.Engine;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace C64AssemblerStudio.Desktop;

public static class ContainerConfiguration
{
    public static IServiceCollection Configure(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
             .Build();

        bool messagesHistory = config.GetValue<bool>("Application:MessagesHistory", false);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddNLog(config);
        });
        services
	        .AddEngine(messagesHistory)
	        .AddCore()
	        .AddSingleton<ISystemInfo, SystemInfo>()
	        .AddSingleton<ISystemDialogs, SystemDialogs>()
	        .AddSingleton<IDockFactory, DockFactory>()
	        .AddSingleton<IGuiServices, GuiServices>()
	        .AddSingleton<IUiServices, UiServices>();
        //services.AddAcme();
        //services.AddOscar64();
        return services;
    }
}