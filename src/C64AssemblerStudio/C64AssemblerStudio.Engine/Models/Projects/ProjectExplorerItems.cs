using System.Text;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;

namespace C64AssemblerStudio.Engine.Models.Projects;

public abstract class ProjectItem: NotifiableObject
{
    public required ProjectDirectory? Parent { get; init; }
    public required string Name { get; set; }

    public string GetRelativeDirectory()
    {
        var sb = new StringBuilder();
        if (this is ProjectDirectory)
        {
            sb.Append(Name);
        }
        ProjectDirectory? current = Parent;
        while (current is not null)
        {
            sb.Insert(0, $"{current.Name}{Path.DirectorySeparatorChar}");
            current = current.Parent;
        }

        return sb.ToString();
    }
}

public class ProjectDirectory : ProjectItem
{
    public ObservableCollection<ProjectItem> Items { get; } = new();

    public IEnumerable<ProjectDirectory> GetSubdirectories()
    {
        foreach (var d in Items.OfType<ProjectDirectory>())
        {
            yield return d;
        }
    }
}

public class ProjectFile : ProjectItem
{
    public required FileType FileType { get; set; }
}