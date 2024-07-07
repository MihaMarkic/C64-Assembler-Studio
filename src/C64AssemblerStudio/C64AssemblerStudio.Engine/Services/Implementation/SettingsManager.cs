using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class SettingsManager : ISettingsManager
{
    readonly ILogger<SettingsManager> logger;
    readonly string directory;
    readonly string settingsPath;
    public SettingsManager(ILogger<SettingsManager> logger)
    {
        this.logger = logger;
        directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "C64AssemblerStudio");
        settingsPath = Path.Combine(directory, "settings.json");
    }
    public Settings LoadSettings()
    {
        Settings? result;
        try
        {
            result = Load<Settings>(settingsPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to load settings, will fallback to default");
            result = null;
        }
        return result ?? new Settings();
    }
    public T? Load<T>(string path)
        where T : class
    {
        T? result = null;
        if (File.Exists(path))
        {
            try
            {
                string content = File.ReadAllText(path);
                result = JsonSerializer.Deserialize<T>(content);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to load {typeof(T).Name}");
                throw;
            }
        }
        return result;
    }
    public void Save(Settings settings) => Save(settings, settingsPath, true);
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
            logger.LogError(ex, $"Failed saving settings:{ex.Message}");
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
