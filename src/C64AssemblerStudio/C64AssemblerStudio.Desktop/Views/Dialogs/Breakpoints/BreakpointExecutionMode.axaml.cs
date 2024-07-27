using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models;

namespace C64AssemblerStudio.Desktop.Views.Dialogs.Breakpoints;

public partial class BreakpointExecutionMode : UserControl
{
    public static readonly DirectProperty<BreakpointExecutionMode, BreakpointMode> ModeProperty =
        AvaloniaProperty.RegisterDirect<BreakpointExecutionMode, BreakpointMode>(nameof(Mode),
            o => o.Mode,
            (o, v) =>
            {
                o.Mode = v;
                o.UpdateClasses();
            },
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<BreakpointExecutionMode, bool> IsExecEnabledProperty =
        AvaloniaProperty.RegisterDirect<BreakpointExecutionMode, bool>(nameof(IsExecEnabled),
            o => o.IsExecEnabled,
            (o, v) => o.IsExecEnabled = v,
            defaultBindingMode: BindingMode.OneWay);

    public static readonly DirectProperty<BreakpointExecutionMode, bool> IsLoadEnabledProperty =
        AvaloniaProperty.RegisterDirect<BreakpointExecutionMode, bool>(nameof(IsLoadEnabled),
            o => o.IsLoadEnabled,
            (o, v) => o.IsLoadEnabled = v,
            defaultBindingMode: BindingMode.OneWay);

    public static readonly DirectProperty<BreakpointExecutionMode, bool> IsStoreEnabledProperty =
        AvaloniaProperty.RegisterDirect<BreakpointExecutionMode, bool>(nameof(IsStoreEnabled),
            o => o.IsStoreEnabled,
            (o, v) => o.IsStoreEnabled = v,
            defaultBindingMode: BindingMode.OneWay);

    public RelayCommand<BreakpointMode> SetModeCommand { get; }
    private BreakpointMode _mode;
    private bool _isExecEnabled;
    private bool _isLoadEnabled;
    private bool _isStoreEnabled;

    public BreakpointExecutionMode()
    {
        SetModeCommand = new RelayCommand<BreakpointMode>(m => Mode = m);
        InitializeComponent();
        UpdateClasses();
    }

    void UpdateClasses()
    {
        const string selected = "selected";
        ExecButton.Classes.Set(selected, Mode == BreakpointMode.Exec);
        LoadButton.Classes.Set(selected, Mode == BreakpointMode.Load);
        StoreButton.Classes.Set(selected, Mode == BreakpointMode.Store);
    }

    public BreakpointMode Mode
    {
        get => _mode;
        set => SetAndRaise(ModeProperty, ref _mode, value);
    }

    public bool IsExecEnabled
    {
        get => _isExecEnabled;
        set => SetAndRaise(IsExecEnabledProperty, ref _isExecEnabled, value);
    }

    public bool IsLoadEnabled
    {
        get => _isLoadEnabled;
        set => SetAndRaise(IsLoadEnabledProperty, ref _isLoadEnabled, value);
    }

    public bool IsStoreEnabled
    {
        get => _isStoreEnabled;
        set => SetAndRaise(IsStoreEnabledProperty, ref _isStoreEnabled, value);
    }
}