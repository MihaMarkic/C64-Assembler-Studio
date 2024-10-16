namespace C64AssemblerStudio.Engine.Models.Projects;

public class KickAssProject : Project
{
    /// <summary>
    /// Defines the location of library directories, separated by ';'
    /// </summary>
    public string LibDir { get; set; } = string.Empty;

    /// <summary>
    /// Returns <see cref="LibDir"/> as <see cref="ImmutableArray{String}"/>.
    /// </summary>
    public ImmutableArray<string> LibDirArray =>
        [..LibDir.Split(';').Select(d => d.Trim()).Where(d => !string.IsNullOrWhiteSpace(d))];
}