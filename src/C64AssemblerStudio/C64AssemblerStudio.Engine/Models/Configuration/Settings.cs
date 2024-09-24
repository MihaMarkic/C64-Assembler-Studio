using System.ComponentModel;
using System.Data.Common;
using System.Globalization;
using System.Text.Json.Serialization;
using C64AssemblerStudio.Core;
using CommunityToolkit.Diagnostics;

namespace C64AssemblerStudio.Engine.Models.Configuration;

public class Settings : NotifiableObject
{
    public const int MaxRecentProjects = 10;
    public const string DefaultViceAddress = "localhost:6802";
    public const int DeafultViceAddressPort = 6802;

    /// <summary>
    /// User selected path to VICE files.
    /// </summary>
    public string? VicePath { get; set; }

    /// <summary>
    /// Signals that VICE files are contained in ./bin subdirectory.
    /// True by default because of legacy.
    /// </summary>
    public bool ViceFilesInBinDirectory { get; set; } = true;
    public bool ResetOnStop { get; set; }
    public bool IsAutoUpdateEnabled { get; set; }
    /// <summary>
    /// Defines VICE's IP address to bind to.
    /// </summary>
    /// <remarks>When null, it uses default localhost:6502</remarks>
    public string? ViceAddress { get; set; }
    public ObservableCollection<string> RecentProjects { get; set; } = new();
    [JsonIgnore]
    public string? LastAccessedDirectory => RecentProjects.Count > 0 ? RecentProjects[0] : null;
    /// <summary>
    /// Depending on VICE type, exe can be in either root directory or bin sub directory.
    /// This property returns the correct path.
    /// </summary>
    public string? RealVicePath
    {
        get
        {
            if (VicePath is not null)
            {
                return ViceFilesInBinDirectory ? Path.Combine(VicePath, "bin") : VicePath;
            }
            return null;
        }
    }
    public string BinaryMonitorArgument => $"--binarymonitor --binarymonitoraddress ip4://127.0.0.1:{ViceMonitorPort}";

    public ushort ViceMonitorPort
    {
        get
        {
            if (ViceAddress is not null)
            {
                int colon = ViceAddress.IndexOf(':');
                if (colon >= 0)
                {
                    if (ushort.TryParse(ViceAddress.AsSpan()[colon..], out ushort port))
                    {
                        return port;
                    }
                }
            }
            return DeafultViceAddressPort;
        }
    }
    public void AddRecentProject(string path)
    {
        if (!RecentProjects.Contains(path))
        {
            RecentProjects.Insert(0, path);
            if (RecentProjects.Count > 10)
            {
                RecentProjects.RemoveAt(RecentProjects.Count - 1);
            }
        }
        else
        {
            int index = RecentProjects.IndexOf(path);
            if (index > 0)
            {
                string temp = RecentProjects[0];
                RecentProjects[0] = path;
                RecentProjects[index] = temp;
            }
        }
    }
}
