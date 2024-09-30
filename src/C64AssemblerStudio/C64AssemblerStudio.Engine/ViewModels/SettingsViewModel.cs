using System.Collections;
using System.ComponentModel;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.BindingValidators;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Models.SystemDialogs;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public sealed class SettingsViewModel : OverlayContentViewModel, INotifyDataErrorInfo
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly Globals _globals;
    private readonly ISettingsManager _settingsManager;
    private readonly ISystemDialogs _systemDialogs;
    public Settings Settings => _globals.Settings;
    private readonly ErrorHandler _errorHandler;
    public bool IsVicePathGood { get; private set; }

    public string? ViceAddress
    {
        get => _ipAddressValidator.Text; 
        set => _ipAddressValidator.Update(value);
    }
    public RelayCommand VerifyValuesCommand { get; }
    public RelayCommandAsync OpenViceDirectoryCommand { get; }
    private readonly IpAddressValidator _ipAddressValidator;
    bool INotifyDataErrorInfo.HasErrors => _errorHandler.HasErrors;
    event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
    {
        add => _errorHandler.ErrorsChanged += value;
        remove => _errorHandler.ErrorsChanged -= value;
    }
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => _errorHandler.GetErrors(propertyName);
    public SettingsViewModel(ILogger<SettingsViewModel> logger, Globals globals, IDispatcher dispatcher,
        ISettingsManager settingsManager, ISystemDialogs systemDialogs, IServiceScope serviceScope) : base(dispatcher)
    {
        _logger = logger;
        _globals = globals;
        _settingsManager = settingsManager;
        _systemDialogs = systemDialogs;
        globals.Settings.PropertyChanged += Settings_PropertyChanged;
        VerifyValues();
        VerifyValuesCommand = new RelayCommand(VerifyValues);
        _ipAddressValidator = serviceScope.CreateIpAddressValidator(nameof(ViceAddress));
        _ipAddressValidator.Update(Settings.ViceAddress);
        var errorHandlerBuilder = ErrorHandler.CreateBuilder()
            .AddValidator(nameof(ViceAddress), _ipAddressValidator);
        _errorHandler = errorHandlerBuilder.Build();
        _errorHandler.ErrorsChanged += ErrorHandlerOnErrorsChanged;
        OpenViceDirectoryCommand = new RelayCommandAsync(OpenViceDirectoryAsync);
    }

    private void ErrorHandlerOnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        CloseCommand.RaiseCanExecuteChanged();
    }
    
    private async Task OpenViceDirectoryAsync()
    {
        if (Settings is null)
        {
            throw new Exception("Settings should be loaded at this point");
        }
        var newDirectory =
            await _systemDialogs.OpenDirectoryAsync(new OpenDirectory(Settings.VicePath, "VICE directory selection"));
        var path = newDirectory.SingleOrDefault();
        if (path is not null)
        {
            Settings.VicePath = path;
        }
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
        Settings.ViceAddress = _ipAddressValidator.Text;
        _settingsManager.Save(Settings);
        base.Closing();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Settings.PropertyChanged -= Settings_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
