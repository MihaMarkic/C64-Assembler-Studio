using System.Collections;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
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
    private readonly IOsDependent _osDependent;
    public Settings Settings => _globals.Settings;
    private readonly ErrorHandler _errorHandler;
    public bool IsVicePathGood { get; private set; }
    public bool IsFlatpakInstallationGood { get; private set; }
    public bool IsVerifyingConfig => IsVerifyingFlatpakInstallation;
    public bool IsVerifyingFlatpakInstallation { get; private set; }
    public bool AreLibrariesGood { get; private set; }

    public string? ViceAddress
    {
        get => _ipAddressValidator.Text; 
        set => _ipAddressValidator.Update(value);
    }
    public RelayCommandAsync VerifyValuesCommand { get; }
    public RelayCommandAsync OpenViceDirectoryCommand { get; }
    private readonly IpAddressValidator _ipAddressValidator;
    bool INotifyDataErrorInfo.HasErrors => _errorHandler.HasErrors;
    event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
    {
        add => _errorHandler.ErrorsChanged += value;
        remove => _errorHandler.ErrorsChanged -= value;
    }
    public LibrariesEditorViewModel LibrariesEditor { get; private set; }
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => _errorHandler.GetErrors(propertyName);

    public SettingsViewModel(ILogger<SettingsViewModel> logger, Globals globals,
        LibrariesEditorViewModel librariesEditor, IDispatcher dispatcher,
        ISettingsManager settingsManager, ISystemDialogs systemDialogs, IServiceScope serviceScope,
        IOsDependent osDependent) : base(dispatcher)
    {
        _logger = logger;
        _globals = globals;
        _settingsManager = settingsManager;
        _systemDialogs = systemDialogs;
        _osDependent = osDependent;
        globals.Settings.PropertyChanged += Settings_PropertyChanged;
        LibrariesEditor = librariesEditor;
        LibrariesEditor.Init(Settings.Libraries.Values);

        _ = VerifyValues();
        VerifyValuesCommand = new RelayCommandAsync(VerifyValues);
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

    protected override void OnPropertyChanged(string? name = null)
    {
        switch (name)
        {
            case nameof(IsVerifyingConfig):
                if (!IsVerifyingConfig)
                {
                    CloseCommand.RaiseCanExecuteChanged();
                }
                break;
        }
        base.OnPropertyChanged(name);
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
            case nameof(Settings.StartType):
                _ = VerifyViceInstallation();
                break;
        }
    }

    private async Task VerifyValues()
    {
        await VerifyViceInstallation();
    }

    private async Task VerifyViceInstallation()
    {
        switch (Settings.StartType)
        {
            case ViceStartType.File:
                VerifyVicePath();
                break;
            case ViceStartType.Flatpak:
                await VerifyFlatpakViceInstallationAsync();
                break;
        }

        AreLibrariesGood = LibrariesEditor.VerifyLibraries();
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
            IsVicePathGood = Directory.GetFiles(pathToVerify, _osDependent.ViceExeName).Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed checking VICE directory validity");
            IsVicePathGood = false;
        }
    }

    private async Task VerifyFlatpakViceInstallationAsync()
    {
        IsVerifyingFlatpakInstallation = true;
        var psi = new ProcessStartInfo("flatpak", "info net.sf.VICE");
        psi.RedirectStandardError = psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        try
        {
            using var proc = Process.Start(psi);
            if (proc is null)
            {
                IsFlatpakInstallationGood = false;
                return;
            }

            var error = await proc.StandardError.ReadToEndAsync();
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            IsFlatpakInstallationGood = proc.ExitCode == 0;
        }
        catch
        {
            IsFlatpakInstallationGood = false;
        }
        finally
        {
            IsVerifyingFlatpakInstallation = false;
        }
    }

    protected override bool CanClose()
    {
        return !_errorHandler.HasErrors && !IsVerifyingConfig;
    }

    protected override async Task ClosingAsync(CancellationToken ct = default)
    {
        Settings.ViceAddress = _ipAddressValidator.Text;
        // first update libraries order
        for (int i = 0; i < LibrariesEditor.Libraries.Count; i++)
        {
            LibrariesEditor.Libraries[i].Order = i;
        }
        Settings.Libraries = LibrariesEditor.Libraries.ToFrozenDictionary(l => l.Name, l => l);
        _settingsManager.Save(Settings);
        await base.ClosingAsync(ct);
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
