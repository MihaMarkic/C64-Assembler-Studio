using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Services.Implementation;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class RegistersViewModel : NotifiableObject, IToolView
{
    private readonly ILogger<RegistersViewModel> _logger;

    private readonly RegistersMapping _mapping;
    // private readonly CommandsManager _commandsManager;
    private readonly IDispatcher _dispatcher;
    public event EventHandler? RegistersUpdated;
    public Registers6510 Current { get; private set; } = Registers6510.Empty;
    public Registers6510 Previous { get; private set; } = Registers6510.Empty;

    // public bool IsLoadingRegisters { get; private set; }
    public byte? PcRegisterId { get; private set; }
    public string Header => "Registers";

    // public RelayCommandAsync UpdateCommand { get; }
    public RegistersViewModel(ILogger<RegistersViewModel> logger, RegistersMapping mapping,
        IDispatcher dispatcher)
    {
        this._logger = logger;
        this._mapping = mapping;
        this._dispatcher = dispatcher;
        // _commandsManager = new CommandsManager(this, new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext()));
        // UpdateCommand = _commandsManager.CreateRelayCommandAsync(Update, () => !IsLoadingMappings && IsLoadingRegisters);
    }

    protected void OnRegistersUpdated(EventArgs e) => RegistersUpdated?.Invoke(this, e);

    public void Init(RegistersAvailableResponse response)
    {
        _mapping.Init(response);
        PcRegisterId = _mapping.GetRegisterId(Register6510.PC);
    }

    public void UpdateRegistersFromResponse(RegistersResponse response)
    {
        if (_mapping.IsMappingAvailable)
        {
            Previous = Current;
            Current = _mapping.MapValues(response);
            OnRegistersUpdated(EventArgs.Empty);
        }
        else
        {
            _logger.LogError("Mappings are not available");
        }
    }

    public void Reset()
    {
        _mapping.Clear();
        Current = Registers6510.Empty;
        Previous = Registers6510.Empty;
    }

    // internal async Task<bool> SetRegisters((Register6510 RegisterCode, ushort Value) item,
    //     params (Register6510 RegisterCode, ushort Value)[] others)
    // {
    //     var items = ImmutableArray<(Register6510 RegisterCode, ushort Value)>.Empty.Add(item).AddRange(others);
    //     var builder = ImmutableArray.CreateBuilder<RegisterItem>(items.Length);
    //     foreach (var i in items)
    //     {
    //         var registerId = _mapping.GetRegisterId(i.RegisterCode);
    //         if (!registerId.HasValue)
    //         {
    //             _logger.LogError("Failed to get {RegisterCode} register id", i.RegisterCode);
    //             _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Setting start address",
    //                 $"Failed to get {i.RegisterCode} register id"));
    //             return false;
    //         }
    //
    //         builder.Add(new RegisterItem(registerId.Value, i.Value));
    //     }
    //
    //     var registers = builder.ToImmutable();
    //     var command = _viceBridge.EnqueueCommand(new RegistersSetCommand(MemSpace.MainMemory, registers),
    //         resumeOnStopped: true);
    //     var response = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command);
    //     bool success = response is not null;
    //     if (success)
    //     {
    //         await UpdateRegistersFromResponseAsync(response!);
    //     }
    //
    //     return success;
    // }

    // internal async Task<bool> SetStartAddressAsync(ushort address, CancellationToken ct) =>
    //     await SetRegisters(new(Register6510.PC, address));
}