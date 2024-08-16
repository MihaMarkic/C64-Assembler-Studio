using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Models;

namespace C64AssemblerStudio.Engine.ViewModels.Breakpoints;

public enum BreakpointError
{
    None,
    InvalidConditon,
    NoAddressRange,
    ViceFailure
}
public class BreakpointViewModel : NotifiableObject, ICloneable
{
    public bool IsCurrentlyHit { get; set; }
    public bool StopWhenHit { get; set; }
    public bool IsEnabled { get; set; }
    public uint HitCount { get; set; }
    public uint IgnoreCount { get; set; }
    public BreakpointMode Mode { get; set; }
    public BreakpointBind? Bind { get; set; }
    public bool IsPersistent { get; init; } = true;
    public string? Condition { get; set; }

    /// <summary>
    /// Flag that signals breakpoint not enabled due to errors, i.e. global variable not found.
    /// </summary>
    public bool HasErrors => Error != BreakpointError.None;
    public BreakpointError Error { get; set; }
    public string? ErrorText { get; set; }
    public HashSet<BreakpointAddressRange>? AddressRanges { get; set; }
    /// <summary>
    /// Checkpoint number as returned by VICE for each address range.
    /// </summary>
    readonly Dictionary<uint, BreakpointAddressRange> _checkpointNumbers = new();
    public BreakpointViewModel()
    {
        Bind = BreakpointNoBind.Empty;
        AddressRanges = null;        
    }
    public BreakpointViewModel(bool stopWhenHit, bool isEnabled, BreakpointMode mode, BreakpointBind bind, string? condition)
    {
        StopWhenHit = stopWhenHit;
        IsEnabled = isEnabled;
        Mode = mode;
        Bind = bind;
        Condition = condition;
    }
    public void ClearError()
    {
        Error = BreakpointError.None;
        ErrorText = null;
    }
    public void SetError(BreakpointError errorType, string errorText)
    {
        Error = errorType;
        ErrorText = errorText;
    }
    public void ClearCheckpointNumbers()
    {
        _checkpointNumbers.Clear();
        OnPropertyChanged(nameof(CheckpointNumbers));
    }
    public void AddCheckpointNumber(BreakpointAddressRange addressRange, uint checkpointNumber)
    {
        _checkpointNumbers.Add(checkpointNumber, addressRange);
        OnPropertyChanged(nameof(CheckpointNumbers));
    }
    public void RemoveCheckpointNumber(uint checkpointNumber)
    {
        _checkpointNumbers.Remove(checkpointNumber);
        OnPropertyChanged(nameof(CheckpointNumbers));
    }
    public IEnumerable<uint> CheckpointNumbers
    {
        get
        {
            foreach (var cn in _checkpointNumbers.Keys)
            {
                yield return cn;
            }
        }
    }
    public BreakpointBindMode BindMode
    {
        get => Bind switch
        {
            BreakpointLineBind => BreakpointBindMode.Line,
            BreakpointNoBind => BreakpointBindMode.None,
            _ => throw new ArgumentOutOfRangeException(),
        };
        set
        {
            if (value != BindMode)
            {
                Bind = value switch
                {
                    BreakpointBindMode.None => BreakpointNoBind.Empty,
                    BreakpointBindMode.Line => BreakpointLineBind.Empty,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }
    }

    public void MarkDisarmed()
    {
        ClearCheckpointNumbers();
        IsCurrentlyHit = false;
        AddressRanges = null;
        Error = BreakpointError.None;
    }
    object ICloneable.Clone() => Clone();
    public BreakpointViewModel Clone()
    {
        return (BreakpointViewModel)MemberwiseClone();
    }
    /// <summary>
    /// Used to compare detail editing changes.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsChangedFrom(BreakpointViewModel other)
    {
        return !(StopWhenHit == other.StopWhenHit && IsEnabled == other.IsEnabled
            && Mode == other.Mode && Bind == other.Bind
            && string.Equals(Condition, other.Condition, StringComparison.Ordinal));
    }
    internal bool AreCheckpointNumbersEqual(Dictionary<uint, BreakpointAddressRange> other)
    {
        if (_checkpointNumbers.Count != other.Count)
        {
            return false;
        }
        foreach (var p in other)
        {
            if (!_checkpointNumbers.TryGetValue(p.Key, out BreakpointAddressRange? value) || value != p.Value)
            {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// Copies all properties from <paramref name="source"/>.
    /// </summary>
    /// <param name="source"></param>
    public void CopyFrom(BreakpointViewModel source)
    {
        IsCurrentlyHit = source.IsCurrentlyHit;
        StopWhenHit = source.StopWhenHit;
        IsEnabled = source.IsEnabled;
        HitCount = source.HitCount;
        IgnoreCount = source.IgnoreCount;
        Mode = source.Mode;
        Bind = source.Bind;
        AddressRanges = source.AddressRanges;
        Condition = source.Condition;
    }
}
public record BreakpointAddressRange(ushort Start, ushort End);