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

public enum ConditionCompletionType
{
    Register,
    Bank,
    Memspace,
    Label,
}

public record ConditionCompletionSuggestionModel(ConditionCompletionType Type, string Value);

public class BreakpointDetailViewModel : ViewModel, IDialogViewModel<SimpleDialogResult>, INotifyDataErrorInfo
{
    private readonly ILogger<BreakpointDetailViewModel> _logger;
    private readonly Globals _globals;
    private readonly IVice _vice;
    private readonly BreakpointsViewModel _breakpoints;
    private readonly BreakpointViewModel _sourceBreakpoint;
    private readonly ErrorHandler _errorHandler;
    
    bool INotifyDataErrorInfo.HasErrors => _errorHandler.HasErrors;
    event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
    {
        add => _errorHandler.ErrorsChanged += value;
        remove => _errorHandler.ErrorsChanged -= value;
    }
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => _errorHandler.GetErrors(propertyName);

    public string? SaveError { get; private set; }
    public BreakpointViewModel Breakpoint { get; }
    public Action<SimpleDialogResult>? Close { get; set; }
    public RelayCommandAsync SaveCommand { get; }
    public RelayCommandAsync CreateCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ClearBindingCommand { get; }
    public BreakpointDetailDialogMode Mode { get; }
    public bool IsDebugging => _vice.IsDebugging;
    public bool HasCreateButton => Mode == BreakpointDetailDialogMode.Create;
    public bool HasApplyButton =>
        false; // Mode == BreakpointDetailDialogMode.Update; // disabled for now, perhaps enabled in the future

    public bool HasSaveButton => Mode == BreakpointDetailDialogMode.Update;
    public string? StartAddress
    {
        get => _startAddressEntryValidator.Text;
        set => _startAddressEntryValidator.Update(value);
    }
    public string? EndAddress
    {
        get => _endAddressEntryValidator.Text;
        set => _endAddressEntryValidator.Update(value);
    }

    public string? BreakpointConditions
    {
        get => _breakpointConditionsValidator.Text;
        set => _breakpointConditionsValidator.Update(value);
    }
    public bool IsAddressRangeReadOnly => IsBreakpointBound;
    public bool IsBreakpointBound => Breakpoint.Bind is not BreakpointNoBind;
    public bool IsModeEnabled => Breakpoint.Bind is not BreakpointLineBind;
    public bool IsExecModeEnabled => Breakpoint.Bind is BreakpointLineBind || Breakpoint.Bind is BreakpointNoBind;
    public bool IsLoadStoreModeEnabled => Breakpoint.Bind is not BreakpointLineBind;
    public ImmutableArray<SyntaxEditorError> ConditionsErrors { get; private set; }
    public ImmutableArray<SyntaxEditorToken> Tokens { get; private set; }
    
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
        var errorHandlerBuilder = ErrorHandler.CreateBuilder()
            .AddValidator(nameof(StartAddress), _startAddressEntryValidator)
            .AddValidator(nameof(EndAddress), _endAddressEntryValidator)
            .AddValidator(nameof(BreakpointConditions), _breakpointConditionsValidator);
        _errorHandler = errorHandlerBuilder.Build();
        _errorHandler.ErrorsChanged += ErrorHandlerOnErrorsChanged;
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

    private void ErrorHandlerOnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        SaveCommand.RaiseCanExecuteChanged();
        CreateCommand.RaiseCanExecuteChanged();
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

    private void ClearBinding()
    {
        Breakpoint.Bind = BreakpointNoBind.Empty;
        UpdateNoBindAddressesFromViewModel(BreakpointNoBind.Empty);
    }
    void UpdateNoBindAddressesFromViewModel(BreakpointNoBind noBind)
    {
        StartAddress = noBind.StartAddress;
        EndAddress = noBind.EndAddress;
    }

    private bool CanSave() => !_errorHandler.HasErrors && Breakpoint.IsChangedFrom(_sourceBreakpoint);
    private ImmutableArray<ConditionCompletionSuggestionModel> GetLabelsSuggestions()
    {
        var project = (KickAssProjectViewModel)_globals.Project;
        var keys = project.Labels?.Keys;
        if (keys is not null)
        {
            return [
                ..keys.Value.Select(x =>
                    new ConditionCompletionSuggestionModel(ConditionCompletionType.Label, x))
            ];
        }

        return ImmutableArray<ConditionCompletionSuggestionModel>.Empty;
    }

    private ImmutableArray<ConditionCompletionSuggestionModel> GetGenericSuggestions()
    {
        return
        [
            ..C64Globals.Registers
                .Select(x => new ConditionCompletionSuggestionModel(ConditionCompletionType.Register, x))
                .Union(
                    C64Globals.MemspacePrefixes
                        .Select(x => new ConditionCompletionSuggestionModel(ConditionCompletionType.Memspace, x)))
        ];
    }
    /// <summary>
    /// Creates completion suggestions if available. When <param name="text"> is null,
    /// it should provide all available suggestions</param>
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public ImmutableArray<ConditionCompletionSuggestionModel> GetCompletionSuggestions(string? text, bool showForSpace)
    {
        return text switch
        {
            ":" => [
                ..C64Globals.Registers
                    .Select(x => new ConditionCompletionSuggestionModel(ConditionCompletionType.Register, x))
            ],
            "@" => [
                .._vice.BankItemsByName.Keys
                    .Select(x => new ConditionCompletionSuggestionModel(ConditionCompletionType.Bank, x))
            ],
            "." => GetLabelsSuggestions(),
            // combines both registers and memspaces since both can be non-prefixed
            null => GetGenericSuggestions(),
            " " when showForSpace => GetGenericSuggestions(),
            _ => ImmutableArray<ConditionCompletionSuggestionModel>.Empty,
        };
    }
    void Breakpoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _errorHandler.HasChanges = true;
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