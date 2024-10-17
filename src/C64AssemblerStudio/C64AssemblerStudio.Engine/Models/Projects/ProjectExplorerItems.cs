using System.Text;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Engine.Models.Projects;

public abstract class ProjectItem: NotifiableObject
{
    public required ProjectDirectory? Parent { get; init; }
    public required string Name { get; set; }
    public string GetRelativeDirectory()
    {
        var sb = new StringBuilder();
        ProjectDirectory? current = Parent;
        while (current is not null)
        {
            // ProjectLibrary shouldn't be included in relative path, since it has absolute path
            // also it sits on top of chain, hence we exit here
            if (current is ProjectLibrary)
            {
                break;
            }
            sb.Insert(0, $"{current.Name}{Path.DirectorySeparatorChar}");
            current = current.Parent;
        }

        return sb.ToString();
    }
    /// <summary>
    /// Returns top parent which has to be of supertype <see cref="ProjectDirectory"/>.
    /// </summary>
    /// <returns></returns>
    public ProjectDirectory? GetRootDirectory()
    {
        ProjectItem? current = this;
        while (current.Parent is not null)
        {
            current = current.Parent;
        }

        // root has to be a ProjectDirectory instance 
        return current as ProjectLibrary;
    }
    /// <summary>
    /// Compares full path to <param name="other"></param>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsSame(ProjectItem other)
    {
        if (other.GetType() != GetType())
        {
            return false;
        }

        string thisDirectory = GetRelativeDirectory();
        string otherDirectory = other.GetRelativeDirectory();
        if (!thisDirectory.Equals(otherDirectory, OsDependent.FileStringComparison))
        {
            return false;
        }
        return Name.Equals(other.Name, OsDependent.FileStringComparison);
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
    public string GetRelativeFilePath() => Path.Combine(GetRelativeDirectory(), Name);
    public bool CanOpen => FileType == FileType.Assembler;
}

public class ProjectLibraries : ProjectItem
{
    public ObservableCollection<ProjectLibrary> Items { get; } = [];
}

public class ProjectLibrary : ProjectDirectory
{
    public required string AbsolutePath { get; init; }
}