using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace C64AssemblerStudio.Desktop.Views;

public class DataContextTemplateSelector : IDataTemplate
{
    [Content]
    public Dictionary<Type, IDataTemplate> Templates { get; } = new Dictionary<Type, IDataTemplate>();
    public Control? Build(object? param)
    {
        var key = param?.GetType();
        if (key is not null && Templates.TryGetValue(key, out var template))
        {
            return template.Build(param);
        }
        return null;
    }

    public bool Match(object? data) => true;
}