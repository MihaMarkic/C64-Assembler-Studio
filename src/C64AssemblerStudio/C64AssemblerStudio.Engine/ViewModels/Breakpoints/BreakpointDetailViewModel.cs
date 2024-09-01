using System.Collections;
using System.Collections.Frozen;
using System.ComponentModel;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.BindingValidators;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyChanged;

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
    public string? StartAddress
    {
        get => _startAddressEntryValidator.TextValue;
        set => _startAddressEntryValidator.Update(value);
    }
    public string? EndAddress
    {
        get => _endAddressEntryValidator.TextValue;
        set => _endAddressEntryValidator.Update(value);
    }

    public string? BreakpointConditions
    {
        get => _breakpointConditionsValidator.TextValue;
        set => _breakpointConditionsValidator.Update(value);
    }
    public bool IsAddressRangeReadOnly => IsBreakpointBound;
    public bool IsBreakpointBound => Breakpoint.Bind is not BreakpointNoBind;
    public bool IsModeEnabled => Breakpoint.Bind is not BreakpointLineBind;
    public bool IsExecModeEnabled => Breakpoint.Bind is BreakpointLineBind || Breakpoint.Bind is BreakpointNoBind;
    public bool IsLoadStoreModeEnabled => Breakpoint.Bind is not BreakpointLineBind;
    public ImmutableArray<SyntaxEditorError> ConditionsErrors { get; private set; }
    public ImmutableArray<SyntaxEditorToken> Tokens { get; private set; }
    private readonly FrozenDictionary<string, ImmutableArray<IBindingValidator>> _validators;
    private readonly AddressEntryValidator _startAddressEntryValidator;
    private readonly AddressEntryValidator _endAddressEntryValidator;
    private readonly BreakpointConditionsValidator _breakpointConditionsValidator;
    public BreakpointDetailViewModel(ILogger<BreakpointDetailViewModel> logger, IServiceScope serviceScope,
        Globals globals, IVice vice,
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
        _startAddressEntryValidator = serviceScope.CreateAddressEntryValidator(nameof(StartAddress), isMandatory: true);
        _endAddressEntryValidator = serviceScope.CreateAddressEntryValidator(nameof(EndAddress), isMandatory: false);
        _breakpointConditionsValidator = serviceScope.CreateBreakpointConditionValidator(nameof(BreakpointConditions));
        var validatorsBuilder = new Dictionary<string, ImmutableArray<IBindingValidator>>
        {
            { nameof(StartAddress), [_startAddressEntryValidator] },
            { nameof(EndAddress), [_endAddressEntryValidator] },
            { nameof(BreakpointConditions), [_breakpointConditionsValidator] }
        };
        _validators = validatorsBuilder.ToFrozenDictionary();
        // bind all validators
        foreach (var validator in _validators.Values.SelectMany(a => a))
        {
            validator.HasErrorsChanged += ValidatorHasErrorsChanged;
        }
        _breakpointConditionsValidator.PropertyChanged += BreakpointConditionsValidatorOnPropertyChanged;
        switch (Breakpoint.Bind)
        {
            case BreakpointNoBind noBind:
                UpdateNoBindAddressesFromViewModel(noBind);
                break;
        }

        _vice.PropertyChanged += ViceOnPropertyChanged;
        ConditionsErrors = [];
        Tokens = [];
        _breakpointConditionsValidator.Update(Breakpoint.Condition);
    }

    private void BreakpointConditionsValidatorOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_breakpointConditionsValidator.GrammarErrors):
                ConditionsErrors = _breakpointConditionsValidator.GrammarErrors;
                break;
            case nameof(_breakpointConditionsValidator.Tokens):
                Tokens = _breakpointConditionsValidator.Tokens;
                break;
        }
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
    [SuppressPropertyChangedWarnings]
    void OnErrorsChanged(DataErrorsChangedEventArgs e) => ErrorsChanged?.Invoke(this, e);
    void ValidatorHasErrorsChanged(object? sender, EventArgs e)
    {
        var validator = (IBindingValidator)sender!;
        HasErrors = _validators.Values
            .SelectMany(a => a)
            .Any(v => v.HasErrors);
        OnErrorsChanged(new DataErrorsChangedEventArgs(validator.SourcePropertyName));
    }
    
    public IEnumerable GetErrors(string? propertyName)
    {
        if (!string.IsNullOrEmpty(propertyName) && _validators.TryGetValue(propertyName, out var propertyValidators))
        {
            var errors = new List<string>();
            foreach (var pv in propertyValidators)
            {
                errors.AddRange(pv.Errors);
            }
            HasErrors = errors.Count > 0;
            return errors.ToImmutableArray();
        }
        else
        {
            return Enumerable.Empty<string>();
        }
    }
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
                // clear no bind (address) validators when type is changed
                if (Breakpoint.Bind is not BreakpointNoBind)
                {
                    _startAddressEntryValidator.Clear();
                    _endAddressEntryValidator.Clear();
                }
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
            case nameof(StartAddress):
            case nameof(EndAddress):
                SaveCommand.RaiseCanExecuteChanged();
                CreateCommand.RaiseCanExecuteChanged();
                break;
        }

        switch (name)
        {
            case nameof(StartAddress) when StartAddress is not null:
                {
                    var bind = (BreakpointNoBind)Breakpoint.Bind.ValueOrThrow();
                    Breakpoint.Bind = bind with { StartAddress = StartAddress };
                }
                break;
            case nameof(EndAddress):
                {
                    var bind = (BreakpointNoBind)Breakpoint.Bind.ValueOrThrow();
                    Breakpoint.Bind = bind with { EndAddress = EndAddress };
                }
                break;
            case nameof(BreakpointConditions):
                Breakpoint.Condition = BreakpointConditions;
                break;
        }
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