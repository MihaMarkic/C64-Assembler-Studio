using C64AssemblerStudio.Core;

namespace C64AssemblerStudio.Engine.Common;

public class BusyIndicator: NotifiableObject
{
    public class Disposable: IDisposable
    {
        private readonly BusyIndicator _busyIndicator;
        public Disposable(BusyIndicator busyIndicator) => _busyIndicator = busyIndicator;
        public void Dispose() => _busyIndicator.Decrease();
    }
    public bool IsBusy => _counter > 0;
    private int _counter;
    public Disposable Increase()
    {
        _counter++;
        if (_counter == 1)
        {
            OnPropertyChanged(nameof(IsBusy));
        }

        return new Disposable(this);
    }

    public void Decrease()
    {
        _counter--;
        if (_counter == 0)
        {
            OnPropertyChanged(nameof(IsBusy));
        }
        else if (_counter < 0)
        {
            throw new Exception("Counter below zero");
        }
    }
}