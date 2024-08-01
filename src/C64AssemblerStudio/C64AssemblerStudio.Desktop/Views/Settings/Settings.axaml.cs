using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Views.Settings;

partial class Settings : UserControl
{
    public Settings()
    {
        InitializeComponent();
    }

    async void OpenViceDirectory(object sender, RoutedEventArgs e)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow!.StorageProvider;
            var options = new FolderPickerOpenOptions
            {
                Title = "VICE directory selection",
                AllowMultiple = false,
            };
            var viewModel = (SettingsViewModel)DataContext!;
            if (viewModel.Settings.VicePath is not null)
            {
                options.SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(viewModel.Settings.VicePath);
            }
            var result = await storageProvider.OpenFolderPickerAsync(options);
            var path = result.Count > 0 ? result[0].Path: null;
            if (path is not null)
            {
                viewModel.Settings.VicePath = path.LocalPath;
            }
        }
    }
}
