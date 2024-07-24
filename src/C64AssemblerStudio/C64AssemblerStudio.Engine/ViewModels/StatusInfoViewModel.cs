using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace C64AssemblerStudio.Engine.ViewModels;

public class StatusInfoViewModel: ViewModel
{
    public BuildStatus BuildingStatus { get; set; } = BuildStatus.Idle;
    public bool IsBuildingStatusVisible => BuildingStatus != BuildStatus.Idle;
}

public enum BuildStatus
{
    [Display(Description = "Building")]
    Building,
    [Display(Description = "Idle")]
    Idle,
    [Display(Description = "Build Success")]
    Success,
    [Display(Description = "Build Failure")]
    Failure
}