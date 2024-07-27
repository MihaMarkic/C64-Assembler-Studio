using C64AssemblerStudio.Engine.Models.Configuration;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface ISettingsManager
{
    Task<Settings> LoadSettingsAsync(CancellationToken ct);
    Task<T?> LoadAsync<T>(string path, CancellationToken ct)
        where T : class;
    void Save(Settings settings);
    //BreakpointsSettings LoadBreakpointsSettings(string filePath);
    //void Save(BreakpointsSettings breakpointsSettings, string filePath);
    void Save<T>(T settings, string path, bool createDirectory);
    Task SaveAsync<T>(T settings, string path, bool createDirectory, CancellationToken ct = default);
}
