using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Views;
using Microsoft.Extensions.DependencyInjection;

namespace C64AssemblerStudio.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            using (var globalsScope = IoC.Host.Services.CreateScope())
            {
                var globals = globalsScope.ServiceProvider.GetRequiredService<Globals>();
                globals.Load();
            }
            var scope = IoC.Host.Services.CreateScope();
            
            var viewModel = scope.ServiceProvider.GetRequiredService<MainViewModel>()!;
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}