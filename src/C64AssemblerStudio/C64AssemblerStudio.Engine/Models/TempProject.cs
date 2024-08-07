﻿using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common.Compiler;
using C64AssemblerStudio.Engine.Common;

namespace C64AssemblerStudio.Engine.Models;

public class TempProject : NotifiableObject
{
    public const string StopAtLabelNone = "[None]";
    public string? File { get; set; }
    public string? Directory => Path.GetDirectoryName(File);
    public string? FullPrgPath => IsPrgSet ? Path.Combine(Directory!, PrgPath!) : null;
    public string? BreakpointsSettingsPath => IsPrgSet ? Path.Combine(Directory!, "breakpoints.json") : null;
    public bool IsPrgSet => !string.IsNullOrWhiteSpace(PrgPath);
    //public Pdb? DebugSymbols { get; set; }
    public object? DebugSymbols { get; set; }
    public string Caption => "TODO"; // $"{Globals.AppName} - {File}";
    public string? PrgPath { get; set; }
    /// <summary>
    /// When enabled, the application will be started from first available address
    /// </summary>
    public DebugAutoStartMode AutoStartMode { get; set; }
    public string StopAtLabel { get; set; } = StopAtLabelNone;
    public AssemblerType CompilerType { get; init; }
    public SourceLanguage SourceLanguage { get; init; }
    public ushort? DebugOutputAddress {  get; set; }
    private TempProject()
    { }
    public static TempProject Create(string file, AssemblerType compilerType, SourceLanguage sourceLanguage)
    {
        return new TempProject
        {
            File = file,
            CompilerType = compilerType,
            SourceLanguage = sourceLanguage,
            // 03ff is the default address to listen on
            DebugOutputAddress = 0x03ff,
        };
    }
    public ProjectConfiguration ToConfiguration() => new ProjectConfiguration
    {
        PrgPath = PrgPath,
        AutoStartMode = AutoStartMode,
        StopAtLabel = StopAtLabel == StopAtLabelNone ? null: StopAtLabel,
        CompilerType = CompilerType,
        DebugOutputAddress = DebugOutputAddress,
    };
    public static TempProject FromConfiguration(ProjectConfiguration configuration, SourceLanguage sourceLanguage) => new TempProject
    {
        PrgPath = configuration.PrgPath,
        AutoStartMode = configuration.AutoStartMode,
        StopAtLabel = configuration.StopAtLabel ?? StopAtLabelNone,
        CompilerType = configuration.CompilerType,
        SourceLanguage = sourceLanguage,
        DebugOutputAddress = configuration.DebugOutputAddress,
    };
}

public record ProjectConfiguration
{
    public string? PrgPath { get; init; }
    public AssemblerType CompilerType { get; init;  }
    public DebugAutoStartMode AutoStartMode { get; init; }
    public string? StopAtLabel { get; init; }
    public ushort? DebugOutputAddress { get; init; }
}
