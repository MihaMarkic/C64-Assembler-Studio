using C64AssemblerStudio.Engine.BindingValidators;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.Services.Implementation;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Dialogs;
using C64AssemblerStudio.Engine.ViewModels.Files;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Microsoft.Extensions.DependencyInjection;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.ViceMonitor.Bridge;
[assembly: CLSCompliant(false)]
namespace C64AssemblerStudio.Engine;
public static class IoCRegistrar
{
    public static IServiceCollection AddEngine(this IServiceCollection services, bool messagesHistoryEnabled)
    {
        services
            .AddSingleton<Globals>()
            .AddSingleton<ISettingsManager, SettingsManager>()
            .AddSingleton<StatusInfoViewModel>()
            .AddSingleton<IVice, Vice>()
            .AddSingleton<RegistersMapping>()
            .AddSingleton<IAddressEntryGrammarService, AddressEntryGrammarService>()
            .AddSingleton<IBreakpointConditionGrammarService, BreakpointConditionGrammarService>()
            .AddTransient<BreakpointConditionsListener>()
            .AddSingleton<IServiceFactory, ServiceFactory>()
            .AddSingleton<IParserManager, ParserManager>()
            // ViewModels
            .AddSingleton<MainViewModel>()
            .AddSingleton<StartPageViewModel>()
            .AddScoped<ProjectFilesWatcherViewModel>()
            .AddSingleton<ProjectExplorerViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddSingleton<FilesViewModel>()
            .AddTransient<AssemblerFileViewModel>()
            .AddSingleton<StatusInfoViewModel>()
            .AddSingleton<RegistersViewModel>()
            .AddSingleton<BreakpointsViewModel>()
            .AddSingleton<ViceMemoryViewModel>()
            .AddSingleton<CallStackViewModel>()
            .AddScoped<MemoryViewerViewModel>()
            .AddTransient<AboutViewModel>()
            .AddTransient<LibrariesEditorViewModel>()
            // Tools
            .AddScoped<ErrorMessagesViewModel>()
            .AddScoped<BuildOutputViewModel>()
            .AddScoped<DebugOutputViewModel>()
            .AddSingleton<ErrorsOutputViewModel>()
            // Misc
            .AddTransient<AddressEntryValidator>()
            // Dialogs
            .AddTransient<AddFileDialogViewModel>()
            .AddTransient<AddDirectoryDialogViewModel>()
            .AddTransient<RenameItemDialogViewModel>()
            .AddScoped<KickAssProjectViewModel>()
            .AddSingleton<EmptyProjectViewModel>()
            .AddScoped<SaveFileDialogViewModel>()
            // System
            .AddTransient(sp => sp.CreateScope())
            .AddSingleton<IDispatcher>(
            // uses dispatching from within same thread to all subscriptions by default as most subscribers are running on UI thread
                new Dispatcher(new DispatchContext(DispatchThreading.SameThread)))
            .AddDebugDataProvider()
            .AddViceBridge();
        return services;
    }
}