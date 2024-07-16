using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using C64AssemblerStudio.Desktop.Views.Dialogs;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Views.Main;

partial class MainWindow : Window
{
    //ToolWindow? messagesHistoryWindow;
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }
    public new MainViewModel? DataContext
    {
        get => (MainViewModel?)base.DataContext;
        set => base.DataContext = value;
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        var viewModel = DataContext;
        if (viewModel is not null)
        {
            _ = StartFadeAsync();
            viewModel.ShowCreateProjectFileDialogAsync = ShowCreateProjectFileDialogAsync;
            viewModel.ShowOpenProjectFileDialogAsync = ShowOpenProjectFileDialogAsync;
            //ViewModel.ShowMessagesHistoryContent = ShowMessagesHistory;
            viewModel.CloseApp = Close;
            viewModel.ShowModalDialog = ShowModalDialog;
        }
        base.OnDataContextChanged(e);
    }

    async Task StartFadeAsync()
    {
        var animation = (Animation?)Resources["FadeLoading"];
        if (animation is not null)
        {
            await animation.RunAsync(Loading);
        }
        Loading.IsVisible = false;
        MainContent.IsVisible = true;
    }
    internal void ShowModalDialog(ShowModalDialogMessageCore message)
    {
        var dialog = new ModalDialogWindow
        {
            DataContext = message,
            MinWidth = message.MinSize.Width,
            MinHeight = message.MinSize.Height,
            Width = message.DesiredSize.Width,
            Height = message.DesiredSize.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        dialog.ShowDialog(this);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        ViewModel.IsShiftDown = false;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        ViewModel.IsShiftDown = e.KeyModifiers == KeyModifiers.Shift;
    }
    internal async Task<string?> ShowOpenProjectFileDialogAsync(OpenFileDialogModel model,
        CancellationToken ct)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow!.StorageProvider;
            var options = new FilePickerOpenOptions
            {
                Title = "Open project",
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
                {
                    new (model.Name)
                    {
                        Patterns = new []{ model.Extension }
                    }
                },
            };
            if (model.InitialDirectory is not null)
            {
                options.SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(model.InitialDirectory);
            }
            var result = await storageProvider.OpenFilePickerAsync(options);
            if (result?.Count == 1)
            {
                return result[0].Path.LocalPath;
            }
        }
        return null;
    }
    internal async Task<string?> ShowCreateProjectFileDialogAsync(OpenFileDialogModel model, CancellationToken ct)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow!.StorageProvider;
            var options = new FilePickerSaveOptions
            {
                Title = "Create project",
                DefaultExtension = model.Extension,
                FileTypeChoices = new FilePickerFileType[]
                {
                    new (model.Name)
                    {
                        Patterns = new []{ model.Extension }
                    }
                },
            };
            if (model.InitialDirectory is not null)
            {
                options.SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(model.InitialDirectory);
            }
            var result = await storageProvider.SaveFilePickerAsync(options);
            return result?.Path.LocalPath;
        }
        return null;
    }

    internal void ShowMessagesHistory()
    {
        //if (messagesHistoryWindow is null)
        //{
        //    messagesHistoryWindow = new ToolWindow
        //    {
        //        DataContext = ViewModel.MessagesHistoryViewModel,
        //    };
        //    messagesHistoryWindow.Closed += (_, _) =>
        //    {
        //        messagesHistoryWindow = null;
        //    };
        //    messagesHistoryWindow.Show();
        //}
        //else
        //{
        //    messagesHistoryWindow.WindowState = WindowState.Normal;
        //}
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext!;

    protected override void OnClosed(EventArgs e)
    {
        //messagesHistoryWindow?.Close();
        //Bootstrap.Close();
        base.OnClosed(e);
    }
}
