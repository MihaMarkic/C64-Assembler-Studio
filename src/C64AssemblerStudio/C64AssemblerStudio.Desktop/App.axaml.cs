using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using MainWindow = C64AssemblerStudio.Desktop.Views.MainWindow;

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
            var globals = IoC.Host.Services.GetRequiredService<Globals>();
            globals.Load();
            var scope = IoC.Host.Services.CreateScope();
            
            var viewModel = scope.ServiceProvider.GetRequiredService<MainViewModel>()!;
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
            desktop.ShutdownRequested += (sender, args) =>
            {
                globals.Save();
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}