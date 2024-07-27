using System.Collections;
using System.ComponentModel;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels.Breakpoints;

public enum BreakpointDetailDialogMode
{
    Create,
    Update
}

public class BreakpointDetailViewModel : NotifiableObject, IDialogViewModel<SimpleDialogResult>, INotifyDataErrorInfo
{
    private readonly ILogger<BreakpointDetailViewModel> _logger;
    private readonly Globals _globals;
    private readonly IVice _vice;
    private readonly BreakpointsViewModel _breakpoints;
    private readonly BreakpointViewModel _sourceBreakpoint;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    
    public string? SaveError { get; private set; }
    public BreakpointViewModel Breakpoint { get; }
    public Action<SimpleDialogResult>? Close { get; set; }
    public RelayCommandAsync SaveCommand { get; }
    public RelayCommandAsync CreateCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ClearBindingCommand { get; }
    public BreakpointDetailDialogMode Mode { get; }
    public bool HasChanges { get; private set; }
    public bool HasErrors { get; private set; }
    public bool CanUpdate => HasChanges && !HasErrors;
    public bool IsDebugging => _vice.IsDebugging;
    public bool HasCreateButton => Mode == BreakpointDetailDialogMode.Create;

    public bool HasApplyButton =>
        false; // Mode == BreakpointDetailDialogMode.Update; // disabled for now, perhaps enabled in the future

    public bool HasSaveButton => Mode == BreakpointDetailDialogMode.Update;
    public string? StartAddress { get; set; }
    public string? EndAddress { get; set; }
    public bool IsAddressRangeReadOnly => IsBreakpointBound;
    public bool IsBreakpointBound => Breakpoint.Bind is not BreakpointNoBind;
    public bool IsModeEnabled => Breakpoint.Bind is not BreakpointLineBind;
    public bool IsExecModeEnabled => Breakpoint.Bind is BreakpointLineBind || Breakpoint.Bind is BreakpointNoBind;
    public bool IsLoadStoreModeEnabled => Breakpoint.Bind is not BreakpointLineBind;

    public BreakpointDetailViewModel(ILogger<BreakpointDetailViewModel> logger, Globals globals, IVice vice,
        BreakpointsViewModel breakpoints, BreakpointViewModel breakpoint, BreakpointDetailDialogMode mode)
    {
        _logger = logger;
        _globals = globals;
        _vice = vice;
        _breakpoints = breakpoints;
        _sourceBreakpoint = breakpoint;
        Breakpoint = breakpoint.Clone();
        Mode = mode;
        SaveCommand = new RelayCommandAsync(SaveAsync, CanSave);
        CreateCommand = new RelayCommandAsync(CreateAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        ClearBindingCommand = new RelayCommand(ClearBinding, () => Breakpoint.Bind is not BreakpointNoBind);
        Breakpoint.PropertyChanged += Breakpoint_PropertyChanged;

        switch (Breakpoint.Bind)
        {
            case BreakpointNoBind noBind:
                UpdateNoBindAddressesFromViewModel(noBind);
                break;
        }
        _vice.PropertyChanged += ViceOnPropertyChanged;
    }

    private void ViceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IVice.IsDebugging):
                OnPropertyChanged(nameof(IsDebugging));
                break;
        }
    }

    internal void ClearBinding()
    {
        Breakpoint.Bind = BreakpointNoBind.Empty;
        UpdateNoBindAddressesFromViewModel(BreakpointNoBind.Empty);
    }
    void UpdateNoBindAddressesFromViewModel(BreakpointNoBind noBind)
    {
        StartAddress = noBind.StartAddress;
        EndAddress = noBind.EndAddress;
    }
    internal bool CanSave() => !HasErrors && Breakpoint.IsChangedFrom(_sourceBreakpoint);
    void Breakpoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasChanges = true;
        SaveError = null;
        SaveCommand.RaiseCanExecuteChanged();
        CreateCommand.RaiseCanExecuteChanged();
        switch (e.PropertyName)
        {
            case nameof(BreakpointViewModel.Bind):
                ClearBindingCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(IsBreakpointBound));
                OnPropertyChanged(nameof(IsModeEnabled));
                OnPropertyChanged(nameof(IsAddressRangeReadOnly));
                OnPropertyChanged(nameof(IsExecModeEnabled));
                OnPropertyChanged(nameof(IsLoadStoreModeEnabled));
                // switch (Breakpoint.Bind)
                // {
                //     case BreakpointNoBind noBind:
                //         Breakpoint.AddressRanges = ImmutableHashSet<BreakpointAddressRange>
                //             .Empty.Add(new(noBind.StartAddress, noBind.EndAddress));
                //         break;
                //     case BreakpointGlobalVariableBind variableBind:
                //         Breakpoint.AddressRanges = ImmutableHashSet<BreakpointAddressRange>.Empty;
                //         globalVariableBindValidator.Update(null);
                //         break;
                // };
                // // clear no bind (address) validators when type is changed
                // if (Breakpoint.Bind is not BreakpointNoBind)
                // {
                //     startAddressValidator.Clear();
                //     endAddressValidator.Clear();
                //     endAddressHigherThanStartValidator.Clear();
                // }
                // if (Breakpoint.Bind is not BreakpointGlobalVariableBind)
                // {
                //     GlobalVariable = null;
                // }
                break;
        }
    }
    async Task SaveAsync()
    {
        try
        {
            await _breakpoints.UpdateBreakpointAsync(Breakpoint, _sourceBreakpoint);
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.OK));
        }
        catch (Exception ex)
        {
            SaveError = $"Failed saving breakpoint: {ex.Message}";
        }
    }
    async Task CreateAsync()
    {
        try
        {
            await _breakpoints.AddBreakpointAsync(Breakpoint, CancellationToken.None);
            Close?.Invoke(new SimpleDialogResult(DialogResultCode.OK));
        }
        catch (Exception ex)
        {
            SaveError = $"Failed creating breakpoint: {ex.Message}";
        }
    }
    void Cancel()
    {
        Close?.Invoke(new SimpleDialogResult(DialogResultCode.Cancel));
    }

    protected override void OnPropertyChanged(string name = null!)
    {
        base.OnPropertyChanged(name);
        switch (name)
        {
            case nameof(HasErrors):
                SaveCommand.RaiseCanExecuteChanged();
                CreateCommand.RaiseCanExecuteChanged();
                break;
        }
    }
    public IEnumerable GetErrors(string? propertyName)
    {
        // if (!string.IsNullOrEmpty(propertyName) && validators.TryGetValue(propertyName, out var propertyValidators))
        // {
        //     var errors = new List<string>();
        //     foreach (var pv in propertyValidators)
        //     {
        //         errors.AddRange(pv.Errors);
        //     }
        //     HasErrors = errors.Count > 0;
        //     return errors.ToImmutableArray();
        // }
        // else
        // {
        //     return Enumerable.Empty<string>();
        // }
        return Enumerable.Empty<string>();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vice.PropertyChanged -= ViceOnPropertyChanged;
            Breakpoint.PropertyChanged -= Breakpoint_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}