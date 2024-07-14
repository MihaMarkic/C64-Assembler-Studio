using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace C64AssemblerStudio.Core;

/// <summary>
/// Base class that implements <see cref="INotifyPropertyChanged"/> 
/// </summary>
public abstract class NotifiableObject : DisposableObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName]string name = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Signals more properties have changed
    /// </summary>
    /// <param name="properties"></param>
    protected void OnPropertiesChanged(params string[] properties)
    {
        foreach (var p in properties)
        {
            OnPropertyChanged(p);
        }
    }
}
