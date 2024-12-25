using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Services.Abstract;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public class AboutViewModel: OverlayContentViewModel
{
    private readonly ISystemInfo _systemInfo;
    private readonly IOsDependent _osDependent;
    public RelayCommandWithParameter<ThirdPartyLibrary> OpenLinkCommand { get; }
    public ImmutableArray<ThirdPartyLibrary> Libraries { get; } =
    [
        new("Antlr4", "https://github.com/antlr/antlr4"),
        new(".NET Community Toolkit", "https://github.com/CommunityToolkit/dotnet"),
        new ("FuzzySharp", "https://github.com/JakeBayer/FuzzySharp"),
        new ("Fody/PropertyChanged","https://github.com/Fody/PropertyChanged"),
        new ("Avalonia", "https://avaloniaui.net/"),
        new ("Avalonia/Svg.Skia", "https://github.com/wieslawsoltes/Svg.Skia"),
        new ("Avalonia XAML Behaviors", "https://github.com/AvaloniaUI/Avalonia.Xaml.Behaviors"),
        new ("AvaloniaEdit", "https://github.com/AvaloniaUI/AvaloniaEdit/"),
        new ("NLog.Extensions.Logging", "https://github.com/NLog/NLog.Extensions.Logging"),
        new ("Velopack", "https://github.com/velopack/velopack"),
        new ("AutoFixture", "https://github.com/AutoFixture/AutoFixture"),
        new ("AutoFixture.Community.ImmutableCollections", "https://github.com/Miista/AutoFixture.Community.ImmutableCollections"),
        new ("NSubstitute", "https://nsubstitute.github.io/"),
        new ("NSubstitute.Analyzers", "https://github.com/nsubstitute/NSubstitute.Analyzers"),
        new ("NUnit", "https://nunit.org/"),
        new ("NUnit/Visual Studio Test Adapter", "https://docs.nunit.org/articles/vs-test-adapter/Index.html"),
        new ("Humanizer", "https://github.com/Humanizer/Humanizer"),
    ];

    // ReSharper disable once MemberCanBeProtected.Global
    public AboutViewModel(ISystemInfo systemInfo, IDispatcher dispatcher, IOsDependent osDependent): base(dispatcher)
    {
        _systemInfo = systemInfo;
        _osDependent = osDependent;
        OpenLinkCommand = new RelayCommandWithParameter<ThirdPartyLibrary>(OpenLink);
    }

    private void OpenLink(ThirdPartyLibrary library)
    {
        Process.Start(_osDependent.FileAppOpenName, library.Url);
    }
    public Version Version => _systemInfo.Version;
}

public record ThirdPartyLibrary(string Name, string Url);

public class DesignAboutViewModel : AboutViewModel
{
    public class InternalSystemInfo : ISystemInfo
    {
        public Version Version =>new Version(0, 1, 2);
    }
    public DesignAboutViewModel() : base(new InternalSystemInfo(), null!, null!)
    {
    }
}