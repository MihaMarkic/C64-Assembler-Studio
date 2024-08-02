using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using CommunityToolkit.Diagnostics;
using FuzzySharp;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class CallStackViewModel : ViewModel, IToolView
{
    private readonly ILogger<CallStackViewModel> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly Globals _globals;
    private readonly ViceMemoryViewModel _memoryViewModel;
    private readonly RegistersViewModel _registersViewModel;
    private readonly ProjectExplorerViewModel _projectExplorer;
    public string Header => "Call stack";
    public RelayCommand<SourceCallStackItem> GoToLineCommand { get; }
    public ImmutableArray<CallStackItem> CallStack { get; private set; }
    public CallStackViewModel(ILogger<CallStackViewModel> logger, IDispatcher dispatcher, Globals globals,
        ViceMemoryViewModel memoryViewModel, RegistersViewModel registersViewModel, ProjectExplorerViewModel projectExplorer)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _globals = globals;
        _memoryViewModel = memoryViewModel;
        _registersViewModel = registersViewModel;
        _projectExplorer = projectExplorer;
        CallStack = ImmutableArray<CallStackItem>.Empty;
        GoToLineCommand = new RelayCommand<SourceCallStackItem>(GoToLine, i => i is CallStackItem);
    }
    private void Clear()
    {
        CallStack = ImmutableArray<CallStackItem>.Empty;
    }
    private void GoToLine(CallStackItem? e)
    {
        if (e is not null)
        {
            switch (e)
            {
                case SourceCallStackItem lineItem:
                    var message = new OpenFileMessage(lineItem.File!, Line: lineItem.FileLocation!.Line1, Column: 1,
                        MoveCaret: true);
                    _dispatcher.Dispatch(message);
                    break;
                // case UnknownCallStackItem unknownAddress:
                //     _dispatcher.Dispatch(new OpenAddressMessage(unknownAddress.Address));
                //     break;
            }
        }
    }

    public void Update()
    {
        if (_registersViewModel.Current.SP.HasValue)
        {
            CreateCallStack(_memoryViewModel.Current.Span, _registersViewModel.Current.SP.Value);
        }
    }

    internal void CreateCallStack(ReadOnlySpan<byte> memory, byte sp)
    {
        var builder = ImmutableArray.CreateBuilder<CallStackItem>();
        // 0xF4 is main function entry SP
        byte i = 0xF4;
        while (i >= sp)
        {
            ushort spAddress = (ushort)(0x0100 + i);
            ushort memAddress = BitConverter.ToUInt16([memory[spAddress + 1], memory[spAddress + 2]]);
            if (memAddress >= 2)
            {
                ushort sourceAddress = (ushort)(memAddress - 2);
                // check if instruction could be JSR
                if (IsValidCall(memory, sourceAddress))
                {
                    var result = _projectExplorer.GetExecutionLocation(sourceAddress);

                    ProjectFile? file;
                    FileLocation? fileLocation;
                    if (result.HasValue)
                    {
                        (file, fileLocation) = result.Value;
                    }
                    else
                    {
                        file = null;
                        fileLocation = null;
                    }

                    string code = "";
                    if (file is not null)
                    {
                        var project = (KickAssProjectViewModel)_globals.Project;
                        Guard.IsNotNull(project.AppInfo);
                        Guard.IsNotNull(project.ByteDumpLines);
                        if (project.AppInfo.SourceFiles.TryGetValue(
                                new SourceFilePath(file.GetRelativeFilePath(), IsRelative: true), out var sourceFile))
                        {
                            var line = project.ByteDumpLines.Value
                                .SingleOrDefault(l => l.AssemblyLine.Address <= sourceAddress &&
                                                      l.AssemblyLine.Address + l.AssemblyLine.Data.Length >
                                                      sourceAddress);
                            if (line is not null)
                            {
                                code = line.AssemblyLine?.Description ?? "";
                            }
                        }
                    }

                    builder.Add(new SourceCallStackItem(
                        sourceAddress,
                        fileLocation,
                        file,
                        code));
                    i -= 2;
                }
                else
                {
                    i--;
                }
            }
            else
            {
                _logger.LogError("StackPointer is pointing to sub zero memory address {Address}", memAddress - 2);
                i--;
            }
        }
        CallStack = builder.ToImmutable();
    }

    internal bool IsValidCall(ReadOnlySpan<byte> memory, ushort sourceAddress)
    {
        // for now, it just checks whether JSR was at the calling address
        return memory[sourceAddress] == 0x20;
    }


    public abstract record CallStackItem(ushort Address, FileLocation? FileLocation, ProjectFile? File, string LineText);

    public record SourceCallStackItem(ushort Address, FileLocation? FileLocation, ProjectFile? File, string LineText)
        : CallStackItem(Address, FileLocation, File, LineText);
    // public record SourceCallStackItem(ushort Address,
    //     PdbFile File, PdbFunction Function, PdbLine Line, PdbAssemblyLine? AssemblyLine)
    //     : CallStackItem(Address, Line.LineNumber + 1, File.Path.Path, Function.XName, Line.Text.Trim(), AssemblyLine?.Text.Trim() ?? string.Empty);
    public record UnknownCallStackItem(ushort Address) : CallStackItem(Address, null, null, "[External code]");
}
//
// public class DesignCallStackViewModel : ICallStackViewModel
// {
//     public ImmutableArray<CallStackViewModel.CallStackItem> CallStack =>
//         ImmutableArray<CallStackViewModel.CallStackItem>.Empty
//         .Add(new CallStackViewModel.SourceCallStackItem(
//                 0x20f9, 
//                 PdbFile.Empty with { Path = PdbPath.CreateAbsolute("D:\test.c") },
//                 PdbFunction.Empty with {  XName = "test() -> void" }, 
//                 PdbLine.Empty with { Text = "int i = Initialize();" },
//                 PdbAssemblyLine.Empty with { Text = "JSR Address" }
//             ));
//
//     public RelayCommand<CallStackViewModel.SourceCallStackItem> GoToLineCommand => throw new NotImplementedException();
// }
