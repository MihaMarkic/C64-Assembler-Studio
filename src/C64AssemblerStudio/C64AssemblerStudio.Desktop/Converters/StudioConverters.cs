using Avalonia.Data.Converters;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Converters;

public static class StudioConverters
{
    public static readonly IValueConverter IsBuildStatusVisible =
        new FuncValueConverter<BuildStatus, bool>(x => x != BuildStatus.Idle);

    public static readonly IValueConverter BuildStatusToString =
        new FuncValueConverter<BuildStatus, string?>(bs => bs.GetDisplayText());
}