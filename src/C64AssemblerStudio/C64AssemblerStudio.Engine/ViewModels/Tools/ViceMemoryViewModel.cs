using C64AssemblerStudio.Core;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public interface IViceMemory
{
    ReadOnlyMemory<byte> Current { get; }
    ReadOnlyMemory<byte> Previous { get; }
}
public class ViceMemoryViewModel: ViewModel, IViceMemory
{
    private readonly ILogger<ViceMemoryViewModel> _logger;
    private readonly IDispatcher _dispatcher;
    private byte[] _previousSnapshot = new byte[ushort.MaxValue+1];
    private byte[] _currentSnapshot = new byte[ushort.MaxValue+1];
    public event EventHandler? MemoryContentChanged;
    public ReadOnlyMemory<byte> Current => _currentSnapshot.AsMemory();
    public ReadOnlyMemory<byte> Previous => _previousSnapshot.AsMemory();
    public ViceMemoryViewModel(ILogger<ViceMemoryViewModel> logger, IDispatcher dispatcher)
    {
        this._logger = logger;
        this._dispatcher = dispatcher;
    }
    void OnMemoryContentChanged(EventArgs e) => MemoryContentChanged?.Invoke(this, e);
    public void GetSnapshot(MemoryGetResponse response)
    {
        using (var buffer = response?.Memory ?? throw new Exception("Failed to retrieve base VICE memory"))
        {
            (_currentSnapshot, _previousSnapshot) = (_previousSnapshot, _currentSnapshot);
            Buffer.BlockCopy(buffer.Data, 0, _currentSnapshot, 0, _currentSnapshot.Length);
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(Previous));
            OnMemoryContentChanged(EventArgs.Empty);
        }
    }

    public void UpdateMemory(ushort start, ReadOnlySpan<byte> memory)
    {
        var target = _currentSnapshot.AsSpan().Slice(start, memory.Length);
        memory.CopyTo(target);
        OnPropertyChanged(nameof(Current));
        OnMemoryContentChanged(EventArgs.Empty);
    }

    public ushort GetShortAt(ushort address)
    {
        if (address == ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(address));
        }
        return BitConverter.ToUInt16(_currentSnapshot, address);
    }

    public ReadOnlySpan<byte> GetSpan(ushort start, ushort end)
    {
        if (start == ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }
        if (end == ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(end));
        }
        return _currentSnapshot.AsSpan()[start..end];
    }
}
