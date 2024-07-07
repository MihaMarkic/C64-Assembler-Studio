using C64AssemblerStudio.Engine.Models.Configuration;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface ISettingsManager
{
    Settings LoadSettings();
    T? Load<T>(string path)
        where T : class;
    void Save(Settings settings);
    //BreakpointsSettings LoadBreakpointsSettings(string filePath);
    //void Save(BreakpointsSettings breakpointsSettings, string filePath);
    void Save<T>(T settings, string path, bool createDirectory);
}
