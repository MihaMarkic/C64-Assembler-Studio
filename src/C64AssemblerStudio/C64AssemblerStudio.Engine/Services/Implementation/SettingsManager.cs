using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Docks;
using Dock.Model.Controls;
using Dock.Model.Mvvm.Core;
using Dock.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;
using JsonProperty = Newtonsoft.Json.Serialization.JsonProperty;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class SettingsManager : ISettingsManager
{
    readonly ILogger<SettingsManager> _logger;
    readonly string _settingsPath;
    private readonly string _toolsPath;
    private readonly IServiceProvider _serviceProvider;
    public SettingsManager(ILogger<SettingsManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "C64AssemblerStudioSettings");
        _settingsPath = Path.Combine(directory, "settings.json");
        _toolsPath = Path.Combine(directory, "tools.json");
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
                _logger.LogError(ex, "Failed to load {Type}", typeof(T).Name);
                throw;
            }
        }
        return result;
    }
    public async Task<T?> LoadAsync<T>(string path, JsonSerializerSettings settings, CancellationToken ct)
        where T : class
    {
        T? result = null;
        if (File.Exists(path))
        {
            try
            {
                string content = await File.ReadAllTextAsync(path, ct);
                result = JsonConvert.DeserializeObject<T>(content, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load {Type}", typeof(T).Name);
                throw;
            }
        }
        return result;
    }
    public void Save(Settings settings) => Save(settings, _settingsPath, true);
    private static JsonSerializerSettings CreateToolsJsonSettings() => 
        new ()
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Objects,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new NoDocumentsSerializeContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Converters =
            {
                new KeyValuePairConverter()
            },
        };
    /// <inheritdoc />
    public async Task<IRootDock?> LoadToolsAsync(CancellationToken ct)
    {
        var jsonSettings = CreateToolsJsonSettings();
        using var scope = _serviceProvider.CreateScope();
        // makes sure FilesDocumentDockViewModel is always a singleton
        var additionalConverter = scope.ServiceProvider.GetRequiredService<FilesDocumentDockViewModelConverter>();
        jsonSettings.Converters.Add(additionalConverter);
        var result = await LoadAsync<IRootDock>(_toolsPath, jsonSettings, ct);
        return result;

    }
    public void SaveTools(IRootDock layout)
    {
        var jsonSettings = CreateToolsJsonSettings();
        Save(layout, _toolsPath, jsonSettings, true);
    }

    /// <inheritdoc />
    public void Save<T>(T settings, string path, JsonSerializerSettings serializerSettings, bool createDirectory)
    {
        var content = JsonConvert.SerializeObject(settings, serializerSettings);
        try
        {
            if (createDirectory)
            {
                var directory = Path.GetDirectoryName(path);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }
            }
            File.WriteAllText(path, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed saving settings:{ex.Message}");
            throw new Exception($"Failed saving settings:{ex.Message}", ex);
        }
    }
    
    public void Save<T>(T settings, string path, bool createDirectory)
    {
        var data = JsonSerializer.Serialize(settings,
            new JsonSerializerOptions
            {
                IgnoreReadOnlyProperties = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            });
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

public class NoDocumentsSerializeContractResolver : ListContractResolver
{
    public NoDocumentsSerializeContractResolver() : base(typeof(ObservableCollection<>))
    { }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        property.ShouldSerialize = property.PropertyName switch
        {
            nameof(FilesDocumentDockViewModel.VisibleDockables) => 
                instance => instance is not FilesDocumentDockViewModel,
            nameof(FilesDocumentDockViewModel.FocusedDockable) => 
                // don't serialize file documents
                instance => (instance as DockBase)?.FocusedDockable is not FileDocumentViewModel,
            nameof(FilesDocumentDockViewModel.ActiveDockable) => 
                // don't serialize file documents
                instance => (instance as DockBase)?.ActiveDockable is not FileDocumentViewModel,
            _ => property.ShouldSerialize
        };

        return property;
    }
}

/// <summary>
/// Always returns singleton when deserializing.
/// </summary>
/// <remarks>A singleton is required, otherwise client won't be able to reference correct version</remarks>
public class FilesDocumentDockViewModelConverter : CustomCreationConverter<FilesDocumentDockViewModel>
{
    private readonly FilesDocumentDockViewModel _filesDocumentDockViewModel;

    public FilesDocumentDockViewModelConverter(FilesDocumentDockViewModel filesDocumentDockViewModel)
    {
        _filesDocumentDockViewModel = filesDocumentDockViewModel;
    }

    public override FilesDocumentDockViewModel Create(Type objectType)
    {
        return _filesDocumentDockViewModel;
    }
}