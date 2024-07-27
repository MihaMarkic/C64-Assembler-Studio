using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class SettingsManager : ISettingsManager
{
    readonly ILogger<SettingsManager> _logger;
    readonly string _settingsPath;
    public SettingsManager(ILogger<SettingsManager> logger)
    {
        this._logger = logger;
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "C64AssemblerStudio");
        _settingsPath = Path.Combine(directory, "settings.json");
    }
    public async Task<Settings> LoadSettingsAsync(CancellationToken ct)
    {
        Settings? result;
        try
        {
            result = await LoadAsync<Settings>(_settingsPath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load settings, will fallback to default");
            result = null;
        }
        return result ?? new Settings();
    }
    public async Task<T?> LoadAsync<T>(string path, CancellationToken ct)
        where T : class
    {
        T? result = null;
        if (File.Exists(path))
        {
            try
            {
                string content = await File.ReadAllTextAsync(path, ct);
                result = JsonSerializer.Deserialize<T>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load {typeof(T).Name}");
                throw;
            }
        }
        return result;
    }
    public void Save(Settings settings) => Save(settings, _settingsPath, true);
    public void Save<T>(T settings, string path, bool createDirectory)
    {
        var data = JsonSerializer.Serialize(settings);
        try
        {
            if (createDirectory)
            {
                string? directory = Path.GetDirectoryName(path);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }
            }
            File.WriteAllText(path, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed saving settings:{ex.Message}");
            throw new Exception($"Failed saving settings:{ex.Message}", ex);
        }
    }
    public async Task SaveAsync<T>(T settings, string path, bool createDirectory, CancellationToken ct = default)
    {
        var data = JsonSerializer.Serialize(settings);
        try
        {
            if (createDirectory)
            {
                string? directory = Path.GetDirectoryName(path);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }
            }
            await File.WriteAllTextAsync(path, data, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed saving settings:{ex.Message}");
            throw new Exception($"Failed saving settings:{ex.Message}", ex);
        }
    }
    //public void Save(BreakpointsSettings breakpointsSettings, string filePath) => Save(breakpointsSettings, filePath, false);
    //public BreakpointsSettings LoadBreakpointsSettings(string filePath)
    //{
    //    BreakpointsSettings? result;
    //    try
    //    {
    //        result = Load<BreakpointsSettings>(filePath);
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex, $"Failed to load breakpoints settings, will fallback to default");
    //        result = null;
    //    }
    //    return result ?? BreakpointsSettings.Empty;
    //}
}
