using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.Services.Implementation;

namespace C64AssemblerStudio.Engine.ViewModels;

public class StatusInfoViewModel : ViewModel
{
    private readonly IVice _vice;
    public BuildStatus BuildingStatus { get; set; } = BuildStatus.Idle;
    public DebuggingStatus DebuggingStatus { get; set; } = DebuggingStatus.Idle;
    public int? EditorCaretLine { get; set; }
    public int? EditorCaretColumn { get; set; }
    public bool IsViceConnected => _vice.IsConnected;
    public string RunCommandTitle => _vice.IsDebugging ? "Continue" : "Run";

    /// <summary>
    /// Only for design time support.
    /// </summary>
    protected StatusInfoViewModel()
    {
        _vice = default!;
    }
    public StatusInfoViewModel(IVice vice)
    {
        _vice = vice;
        _vice.PropertyChanged += ViceOnPropertyChanged;
    }

    private void ViceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IVice.IsConnected):
                OnPropertyChanged(nameof(IsViceConnected));
                break;
            case nameof(IVice.IsDebugging):
                DebuggingStatus = _vice.IsDebugging
                    ? (_vice.IsPaused ? DebuggingStatus.Paused : DebuggingStatus.Debugging)
                    : DebuggingStatus.Idle;
                break;
            case nameof(IVice.IsPaused):
                if (_vice.IsDebugging)
                {
                    DebuggingStatus = _vice.IsPaused ? DebuggingStatus.Paused: DebuggingStatus.Debugging;
                }
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vice.PropertyChanged -= ViceOnPropertyChanged;
        }

        base.Dispose(disposing);
    }
}

public enum BuildStatus
{
    Building,
    Idle,
    Success,
    Failure
}

public enum DebuggingStatus
{
    Idle,
    WaitingForConnection,
    Debugging,
    Paused
}

public class DesignStatusInfoViewModel : StatusInfoViewModel
{
    public DesignStatusInfoViewModel()
    {
        DebuggingStatus = DebuggingStatus.Debugging;
        BuildingStatus = BuildStatus.Building;
        EditorCaretLine = 3;
        EditorCaretColumn = 15;
    }
}