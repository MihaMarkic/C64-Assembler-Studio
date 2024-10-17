using System.Text;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;

namespace C64AssemblerStudio.Engine.Models.Projects;

public abstract class ProjectItem: NotifiableObject
{
    public required ProjectDirectory? Parent { get; init; }
    public required string Name { get; set; }
    public string RelativeDirectory
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var p in GetParents(includeRoot: false))
            {
                sb.Insert(0, $"{p.Name}{Path.DirectorySeparatorChar}");
            }

            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Returns absolute path for directory below this one.
    /// </summary>
    /// <returns></returns>
    public virtual string AbsoluteDirectory
    {
        get
        {
            var relative = RelativeDirectory;
            var root = Root.ValueOrThrow();
            return Path.Combine(root.AbsoluteRootPath, relative);
        }
    }
    /// <summary>
    /// Returns full path.
    /// </summary>
    /// <returns></returns>
    public virtual string AbsolutePath => Path.Combine(AbsoluteDirectory, Name);

    public ProjectRootDirectory? Root => (ProjectRootDirectory?)GetParents(includeRoot: true).LastOrDefault();

    public IEnumerable<ProjectDirectory> GetParents(bool includeRoot)
    {
        ProjectDirectory? current = Parent;
        while (current is not null)
        {
            if (!includeRoot && current.Parent is null)
            {
                break;
            }
            yield return current;
            current = current.Parent;
        }
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

        string thisDirectory = RelativeDirectory;
        string otherDirectory = other.RelativeDirectory;
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
    public string GetRelativeFilePath() => Path.Combine(RelativeDirectory, Name);
    public bool CanOpen => FileType == FileType.Assembler;
}

public class ProjectLibraries : ProjectItem
{
    public ObservableCollection<ProjectLibrary> Items { get; } = [];
}

public abstract class ProjectRootDirectory : ProjectDirectory
{
    public required string AbsoluteRootPath { get; init; }
    public override string AbsolutePath => AbsoluteRootPath;
    public override string AbsoluteDirectory => AbsoluteRootPath;
}

public class ProjectLibrary : ProjectRootDirectory
{ }
public class ProjectRoot: ProjectRootDirectory
{ }