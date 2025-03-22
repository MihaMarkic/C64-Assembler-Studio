using System.Diagnostics;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Services.Implementation;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public class StartPageViewModel: ScopedViewModel
{
    private readonly INavigationManager _navigationManager;
    private readonly IDispatcher _dispatcher;
    public bool HasRecentProjects { get; set; }
    public string? FullPath { get; set; }
    public RelayCommandAsync LoadLastProjectCommand { get; }

    public StartPageViewModel(INavigationManager navigationManager, IDispatcher dispatcher)
    {
        _navigationManager = navigationManager;
        _dispatcher = dispatcher;
        LoadLastProjectCommand = new(LoadLastProject, () => HasRecentProjects);
    }

    async Task LoadLastProject()
    {
        var message = new LoadProjectMessage(FullPath!);
        await _dispatcher.DispatchAsync(message);
        _navigationManager.Navigate(Navigation.Home);
    }
}

public class DesignStartPageViewModel : StartPageViewModel
{
    public DesignStartPageViewModel(): base(new MockNavigationManager(), new MockDispatcher())
    {
        HasRecentProjects = true;
        FullPath = @"D:\TestPath\Project\Dude.cas";
    }
    public class MockNavigationManager: INavigationManager
    {
        public void Navigate(Navigation target)
        {
            throw new NotImplementedException();
        }
    }

    public class MockDispatcher : IDispatcher
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task DispatchAsync<TKey, TMessage>(TKey key, TMessage message, DispatchContext? context = null,
            CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task DispatchAsync<TMessage>(TMessage message, DispatchContext? context = null,
            CancellationToken ct = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public void Dispatch<TKey, TMessage>(TKey key, TMessage message, DispatchContext? context = null)
        {
            throw new NotImplementedException();
        }

        public void Dispatch<TMessage>(TMessage message, DispatchContext? context = null)
        {
            throw new NotImplementedException();
        }

        public ISubscription Subscribe<TKey, TMessage>(TKey key, Action<TKey, TMessage> handler, string? name = null)
        {
            throw new NotImplementedException();
        }

        public ISubscription Subscribe<TKey, TMessage>(TKey key, Func<TKey, TMessage, CancellationToken, Task> handler, string? name = null)
        {
            throw new NotImplementedException();
        }

        public ISubscription Subscribe<TMessage>(Action<TMessage> handler, string? name = null)
        {
            throw new NotImplementedException();
        }

        public ISubscription Subscribe<TMessage>(Func<TMessage, CancellationToken, Task> handler, string? name = null)
        {
            throw new NotImplementedException();
        }

        public Task<TMessage> GetMessageAsync<TKey, TMessage>(TKey key, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<TMessage> GetMessageAsync<TMessage>(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}