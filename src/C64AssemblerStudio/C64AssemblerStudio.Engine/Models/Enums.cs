﻿using System.ComponentModel.DataAnnotations;

namespace C64AssemblerStudio.Engine.Models;

public enum DebugAutoStartMode
{
    [Display(Description = "No auto start")]
    None,
    [Display(Description = "Auto starts using VICE")]
    Vice,
    [Display(Description = "Auto starts at 'start' label address")]
    AtAddress,
}

public enum BreakpointMode
{
    Exec,
    Load,
    Store
}

public enum BreakpointBindMode
{
    None,
    Line,
    Label,
    GlobalVariable,
}

[Flags]
public enum DialogButton
{
    OK,
    Cancel,
    Save,
    DoNotSave,
}

public enum DialogResultCode
{
    OK,
    Cancel
}
