using System.Collections;
using System.ComponentModel;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.BindingValidators;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public sealed class SettingsViewModel : OverlayContentViewModel, INotifyDataErrorInfo
{
    readonly ILogger<SettingsViewModel> _logger;
    readonly Globals _globals;
    readonly ISettingsManager _settingsManager;
    public Settings Settings => _globals.Settings;
    private readonly ErrorHandler _errorHandler;
    public bool IsVicePathGood { get; private set; }

    public string? ViceAddress
    {
        get => _ipAddressValidator.Text; 
        set => _ipAddressValidator.Update(value);
    }
    public RelayCommand VerifyValuesCommand { get; }
    private readonly IpAddressValidator _ipAddressValidator;
    bool INotifyDataErrorInfo.HasErrors => _errorHandler.HasErrors;
    event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
    {
        add => _errorHandler.ErrorsChanged += value;
        remove => _errorHandler.ErrorsChanged -= value;
    }
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => _errorHandler.GetErrors(propertyName);
    public SettingsViewModel(ILogger<SettingsViewModel> logger, Globals globals, IDispatcher dispatcher,
        ISettingsManager settingsManager, IServiceScope serviceScope) : base(dispatcher)
    {
        _logger = logger;
        _globals = globals;
        _settingsManager = settingsManager;
        globals.Settings.PropertyChanged += Settings_PropertyChanged;
        VerifyValues();
        VerifyValuesCommand = new RelayCommand(VerifyValues);
        _ipAddressValidator = serviceScope.CreateIpAddressValidator(nameof(ViceAddress));
        _ipAddressValidator.Update(_globals.Settings.ViceAddress);
        var errorHandlerBuilder = ErrorHandler.CreateBuilder()
            .AddValidator(nameof(ViceAddress), _ipAddressValidator);
        _errorHandler = errorHandlerBuilder.Build();
        _errorHandler.ErrorsChanged += ErrorHandlerOnErrorsChanged;
    }

    private void ErrorHandlerOnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        CloseCommand.RaiseCanExecuteChanged();
    }

    void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.VicePath):
                VerifyVicePath();
                break;
        }
    }

    private void VerifyValues()
    {
        VerifyVicePath();
    }

    private void VerifyVicePath()
    {
        if (string.IsNullOrWhiteSpace(Settings.VicePath))
        {
            IsVicePathGood = false;
            return;
        }
        string binPath = Path.Combine(Settings.VicePath, "bin");
        Settings.ViceFilesInBinDirectory = Directory.Exists(binPath);
        string pathToVerify = Settings.ViceFilesInBinDirectory ? binPath : Settings.VicePath;
        try
        {
            IsVicePathGood = Directory.GetFiles(pathToVerify, OsDependent.ViceExeName).Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed checking VICE directory validity");
            IsVicePathGood = false;
        }
    }

    protected override bool CanClose()
    {
        return !_errorHandler.HasErrors;
    }

    protected override void Closing()
    {
        _settingsManager.Save(Settings);
        base.Closing();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _globals.Settings.PropertyChanged -= Settings_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
