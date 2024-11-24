using System.Collections.Frozen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Common.Compiler;
using C64AssemblerStudio.Engine.BindingValidators;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Files;

// ReSharper disable once CheckNamespace
namespace System;

public static class ServiceProviderExtension
{
    public static T CreateScopedSourceFileViewModel<T>(this IServiceProvider serviceProvider, ProjectFile file)
        where T : FileViewModel
    {
        var contentScope = serviceProvider.CreateScope();
        var viewModel = ActivatorUtilities.CreateInstance<T>(contentScope.ServiceProvider, file);
        viewModel.AssignScope(contentScope);
        return viewModel;
    }

    public static AssemblerFileViewModel CreateScopedAssemblerSourceFileViewModel(this IServiceProvider serviceProvider,
        ProjectFile file,
        NullableArgument<FrozenSet<string>> defineSymbols = null)
    {
        var contentScope = serviceProvider.CreateScope();
        var viewModel =
            ActivatorUtilities.CreateInstance<AssemblerFileViewModel>(contentScope.ServiceProvider, file,
                defineSymbols);
        viewModel.AssignScope(contentScope);
        return viewModel;
    }

    //public static DisassemblyViewModel CreateScopedDisassemblyViewModel(this IServiceProvider serviceProvider, ushort address)
    //{
    //    var contentScope = serviceProvider.CreateScope();
    //    var viewModel = ActivatorUtilities.CreateInstance<DisassemblyViewModel>(serviceProvider, address);
    //    viewModel.AssignScope(contentScope);
    //    return viewModel;
    //}
    /// <summary>
    /// Creates an instance of <typeparamref name="T"/> within a new scope and stores scope in the newly created instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serviceProvider"></param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public static T CreateScopedContent<T>(this IServiceProvider serviceProvider)
        where T : ScopedViewModel
    {
        var contentScope = serviceProvider.CreateScope();
        T viewModel = contentScope.ServiceProvider.GetRequiredService<T>() ?? throw new Exception($"Failed creating {typeof(T).Name} ViewModel");
        viewModel.AssignScope(contentScope);
        return viewModel;
    }
    public static BreakpointDetailViewModel CreateScopedBreakpointDetailViewModel(this IServiceScope serviceScope,
        BreakpointViewModel breakpointViewModel, BreakpointDetailDialogMode mode)
    {
        var viewModel = ActivatorUtilities.CreateInstance<BreakpointDetailViewModel>(serviceScope.ServiceProvider,
            serviceScope.ServiceProvider.GetRequiredService<ILogger<BreakpointDetailViewModel>>(),
            serviceScope.ServiceProvider.GetRequiredService<BreakpointsViewModel>(),
            breakpointViewModel, mode);
        return viewModel;
    }

    public static AddressEntryValidator CreateAddressEntryValidator(this IServiceScope serviceScope,
        string sourcePropertyName,
        bool isMandatory)
    {
        return ActivatorUtilities.CreateInstance<AddressEntryValidator>(serviceScope.ServiceProvider,
            sourcePropertyName, isMandatory);
    }
    public static BreakpointConditionsValidator CreateBreakpointConditionValidator(this IServiceScope serviceScope,
        string sourcePropertyName)
    {
        return ActivatorUtilities.CreateInstance<BreakpointConditionsValidator>(serviceScope.ServiceProvider,
            sourcePropertyName);
    }
    public static IpAddressValidator CreateIpAddressValidator(this IServiceScope serviceScope,
        string sourcePropertyName)
    {
        return ActivatorUtilities.CreateInstance<IpAddressValidator>(serviceScope.ServiceProvider,
            sourcePropertyName);
    }
}
