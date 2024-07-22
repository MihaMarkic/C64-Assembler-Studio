using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public interface IToolView
{
    string Header { get; }
}
public abstract class OutputViewModel<T>: ViewModel, IToolView
{
    protected readonly CommandsManager CommandsManager;
    protected readonly TaskFactory UiFactory;
    public abstract string Header { get; }
    public RelayCommand ClearCommand { get; }
    public ObservableCollection<T> Lines { get; }
    public bool IsEmpty => Lines.Count == 0;

    protected OutputViewModel()
    {
        UiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        CommandsManager = new(this, UiFactory);
        Lines = new ObservableCollection<T>();
        ClearCommand = CommandsManager.CreateRelayCommand(Clear, () => !IsEmpty);
    }

    public void AddLine(T line)
    {
        Lines.Add(line);
        OnPropertyChanged(nameof(IsEmpty));
    }
    public void AddLines(IEnumerable<T> lines)
    {
        foreach (var l in lines)
        {
            Lines.Add(l);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }
    public void Clear()
    {
        Lines.Clear();
        OnPropertyChanged(nameof(IsEmpty));
    }
}