using C64AssemblerStudio.Engine.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine;

public static class IoCRegistrar
{
    public static void AddEngine(this IServiceCollection services, bool messagesHistoryEnabled)
    {
        services
            .AddSingleton<Globals>()
            .AddSingleton<MainViewModel>()
            .AddScoped<SettingsViewModel>()
            .AddSingleton<EmptyProjectViewModel>()
            .AddTransient(sp => sp.CreateScope())
            .AddSingleton<IDispatcher>(
            // uses dispatching from within same thread to all subscriptions by default as most subscribers are running on UI thread
new Dispatcher(new DispatchContext(DispatchThreading.SameThread)));
    }
}