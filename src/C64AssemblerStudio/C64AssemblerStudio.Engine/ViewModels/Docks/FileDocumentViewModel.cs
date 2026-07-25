using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Files;
using Dock.Model.Mvvm.Controls;

namespace C64AssemblerStudio.Engine.ViewModels.Docks;

public class FileDocumentViewModel : Document
{
    private readonly ProjectFileViewModel _fileViewModel;
    private readonly IGuiServices _guiServices;
    public FileDocumentViewModel(IGuiServices guiServices, ProjectFileViewModel viewModel)
    {
        _fileViewModel = viewModel;
        _guiServices = guiServices;
        Id = viewModel.File.AbsolutePath;
        Title = viewModel.File.GetRelativeFilePath();
        CanFloat = false;
        base.Context = viewModel;
    }
    public new ProjectFileViewModel Context => (ProjectFileViewModel)base.Context!;

    /// <summary>
    /// Handles file close.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// Since this call is synchronous and file close operation is not, a little bit of a workaround is required.
    /// </remarks>
    public override bool OnClose()
    {
        using var cts = new CancellationTokenSource();
        bool result = false;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        _fileViewModel.HandlesCloseFileAsync().ContinueWith(t =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        {
            result = t is { IsCompletedSuccessfully: true, Result: true };
            // when FileViewModel will actually close, clears its output
            if (result)
            {
	            Context.ClearErrorsOutput();
            }
            // ReSharper disable once AccessToDisposedClosure
            cts.Cancel();
        }, TaskScheduler.FromCurrentSynchronizationContext());
        
        _guiServices.WaitUntilCancellation(cts.Token);
        return result;
    }
}