using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using MainWindow = C64AssemblerStudio.Desktop.Views.Main.MainWindow;

namespace C64AssemblerStudio.Desktop;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            var globals = IoC.Host.Services.GetRequiredService<Globals>();
            await globals.LoadAsync(CancellationToken.None);
            var scope = IoC.Host.Services.CreateScope();
            var viewModel = scope.ServiceProvider.GetRequiredService<MainViewModel>()!;
            desktop.MainWindow.DataContext = viewModel;
            desktop.ShutdownRequested += (sender, args) =>
            {
                globals.Save();
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}