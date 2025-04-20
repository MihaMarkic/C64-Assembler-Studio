using C64AssemblerStudio.Engine.Models.Configuration;
using Dock.Model.Controls;
using Newtonsoft.Json;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface ISettingsManager
{
    Task<Settings> LoadSettingsAsync(CancellationToken ct);
    Task<T?> LoadAsync<T>(string path, CancellationToken ct)
        where T : class;
    void Save(Settings settings);

    void SaveTools(IRootDock layout);
    //BreakpointsSettings LoadBreakpointsSettings(string filePath);
    //void Save(BreakpointsSettings breakpointsSettings, string filePath);
    void Save<T>(T settings, string path, bool createDirectory);
    Task SaveAsync<T>(T settings, string path, bool createDirectory, CancellationToken ct = default);
    /// <summary>
    /// Saves content using Newtonsoft.Json.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="path"></param>
    /// <param name="serializerSettings"></param>
    /// <param name="createDirectory"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="Exception"></exception>
    /// <remarks>Necessary because Dock uses it.</remarks>
    void Save<T>(T settings, string path, JsonSerializerSettings serializerSettings, bool createDirectory);

    /// <summary>
    /// Loads docking layout.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<IRootDock?> LoadToolsAsync(CancellationToken ct);
}
