using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using System.ComponentModel;
using C64AssemblerStudio.Core;

namespace C64AssemblerStudio.Engine.ViewModels;

public sealed class SettingsViewModel : OverlayContentViewModel
{
    readonly ILogger<SettingsViewModel> _logger;
    readonly Globals _globals;
    readonly ISettingsManager _settingsManager;
    public Settings Settings => _globals.Settings;
    public bool IsVicePathGood { get; private set; }
    public RelayCommand VerifyValuesCommand { get; }
    public SettingsViewModel(ILogger<SettingsViewModel> logger, Globals globals, IDispatcher dispatcher,
        ISettingsManager settingsManager) : base(dispatcher)
    {
        _logger = logger;
        _globals = globals;
        _settingsManager = settingsManager;
        globals.Settings.PropertyChanged += Settings_PropertyChanged;
        VerifyValues();
        VerifyValuesCommand = new RelayCommand(VerifyValues);
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
