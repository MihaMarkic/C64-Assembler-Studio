using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using C64AssemblerStudio.Core;
using Microsoft.Extensions.Options;

namespace C64AssemblerStudio.Engine.Models.Configuration;

/// <summary>
/// Represents a library.
/// </summary>
public class Library : NotifiableObject
{
    /// <summary>
    /// Name of the library.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Absolute path to the library.
    /// </summary>
    public string Path { get; set; } = "";
    /// <summary>
    /// Order of library importance
    /// </summary>
    public int Order { get; set; }
}

// public class LibraryConverterFactory : JsonConverterFactory
// {
//     public override bool CanConvert(Type typeToConvert)
//     {
//         var result = typeToConvert == typeof(FrozenDictionary<string, Library>);
//         return result;
//     }
//     public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
//     {
//         return new LibraryJsonConverter(options);
//     }
// }

public class LibraryJsonConverter : JsonConverter<FrozenDictionary<string, Library>>
{
    private record InternalLibrary(string Path, int Order);

    private readonly JsonConverter<ImmutableDictionary<string, InternalLibrary>>? _converter;

    // ReSharper disable once UnusedMember.Global
    public LibraryJsonConverter()
    { }

    public LibraryJsonConverter(JsonSerializerOptions options)
    {
        _converter = CreateConverter(options);
    }

    private static JsonConverter<ImmutableDictionary<string, InternalLibrary>>
        CreateConverter(JsonSerializerOptions options) =>
        (JsonConverter<ImmutableDictionary<string, InternalLibrary>>)options.GetConverter(
            typeof(ImmutableDictionary<string, InternalLibrary>));

    public override FrozenDictionary<string, Library>? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var converter = _converter ?? CreateConverter(options);
        var j = converter.Read(ref reader, typeof(ImmutableDictionary<string, InternalLibrary>), options)!;
        return j.ToFrozenDictionary(x => x.Key, x => new Library { Name = x.Key, Path = x.Value.Path, Order = x.Value.Order});
    }

    public override void Write(Utf8JsonWriter writer, FrozenDictionary<string, Library> value, JsonSerializerOptions options)
    {
        var converter = _converter ?? CreateConverter(options);
        var j = value.ToImmutableDictionary(v => v.Key, v => new InternalLibrary(v.Value.Path, v.Value.Order));
        converter.Write(writer, j, options);
    }
}