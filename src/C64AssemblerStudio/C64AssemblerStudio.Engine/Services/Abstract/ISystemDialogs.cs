using C64AssemblerStudio.Engine.Models.SystemDialogs;

namespace C64AssemblerStudio.Engine.Services.Abstract;

/// <summary>
/// Service providing access to system dialogs such as OpenFolderPicker
/// </summary>
public interface ISystemDialogs
{
    /// <summary>
    /// Lets user select directory or directories through system UI picker.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<ImmutableArray<string>> OpenDirectoryAsync(OpenDirectory model);
}