using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using C64AssemblerStudio.Engine.Models.SystemDialogs;
using C64AssemblerStudio.Engine.Services.Abstract;

namespace C64AssemblerStudio.Desktop.Services.Implementation;

/// <inheriddoc />
public class SystemDialogs: ISystemDialogs
{
    /// <inheriddoc />
    public async Task<ImmutableArray<string>> OpenDirectoryAsync(OpenDirectory model)
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow!.StorageProvider;
            var options = new FolderPickerOpenOptions
            {
                Title = model.Title,
                AllowMultiple = model.AllowMultiple,
            };
            if (model.Current is not null)
            {
                options.SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(model.Current);
            }

            var result = await storageProvider.OpenFolderPickerAsync(options);
            if (result.Count > 0)
            {
                return [..result.Select(p => p.Path.LocalPath)];
            }
        }

        return ImmutableArray<string>.Empty;
    }
}