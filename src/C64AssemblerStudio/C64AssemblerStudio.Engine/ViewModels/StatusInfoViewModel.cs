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
    public bool IsViceConnected => _vice.IsConnected;
    public string RunCommandTitle => _vice.IsDebugging ? "Continue" : "Run";

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
                DebuggingStatus = _vice.IsDebugging ? DebuggingStatus.Debugging: DebuggingStatus.Idle;
                break;
            case nameof(IVice.IsPaused):
                if (_vice.IsDebugging)
                {
                    DebuggingStatus = DebuggingStatus.Paused;
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
    [Display(Description = "Building")] Building,
    [Display(Description = "Idle")] Idle,

    [Display(Description = "Build Success")]
    Success,

    [Display(Description = "Build Failure")]
    Failure
}

public enum DebuggingStatus
{
    [Display(Description = "Idle")]
    Idle,
    [Display(Description = "Waiting For Connection")]
    WaitingForConnection,
    [Display(Description = "Debugging")]
    Debugging,
    [Display(Description = "Paused")]
    Paused
}