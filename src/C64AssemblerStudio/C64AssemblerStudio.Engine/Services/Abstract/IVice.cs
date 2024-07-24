using System.ComponentModel;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface IVice: INotifyPropertyChanged
{
    bool IsConnected { get; }
    bool IsPaused { get; }
    bool IsDebugging { get; }
    Task ConnectAsync(CancellationToken ct = default);
}