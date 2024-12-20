﻿using System.Collections.Frozen;
using C64AssemblerStudio.Engine.Models;
using Microsoft.Extensions.Logging;
using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class RegistersMapping
{
    readonly ILogger<RegistersMapping> _logger;
    FrozenDictionary<byte, Register6510> _map = FrozenDictionary<byte, Register6510>.Empty;
    FrozenDictionary<Register6510, byte> _inverseMap = FrozenDictionary<Register6510, byte>.Empty;
    public RegistersMapping(ILogger<RegistersMapping> logger)
    {
        this._logger = logger;
    }
    public bool IsMappingAvailable => _map.Count > 0;
    public void Clear()
    {
        _map = FrozenDictionary<byte, Register6510>.Empty;
    }
    public void Init(RegistersAvailableResponse response)
    {
        Dictionary<byte, Register6510> result = new Dictionary<byte, Register6510>();
        foreach (var item in response.Items)
        {
            switch (item.Name)
            {
                case "A":
                    result.Add(item.Id, Register6510.A);
                    break;
                case "X":
                    result.Add(item.Id, Register6510.X);
                    break;
                case "Y":
                    result.Add(item.Id, Register6510.Y);
                    break;
                case "PC":
                    result.Add(item.Id, Register6510.PC);
                    break;
                case "SP":
                    result.Add(item.Id, Register6510.SP);
                    break;
                case "FL":
                    result.Add(item.Id, Register6510.Flags);
                    break;
                case "R3":
                    result.Add(item.Id, Register6510.R3);
                    break;
                case "R4":
                    result.Add(item.Id, Register6510.R4);
                    break;
                case "R5":
                    result.Add(item.Id, Register6510.R5);
                    break;
                case "R6":
                    result.Add(item.Id, Register6510.R6);
                    break;
                case "R7":
                    result.Add(item.Id, Register6510.R7);
                    break;
                case "R8":
                    result.Add(item.Id, Register6510.R8);
                    break;
                case "R9":
                    result.Add(item.Id, Register6510.R9);
                    break;
                case "R10":
                    result.Add(item.Id, Register6510.R10);
                    break;
                case "R11":
                    result.Add(item.Id, Register6510.R11);
                    break;
                case "R12":
                    result.Add(item.Id, Register6510.R12);
                    break;
                case "R13":
                    result.Add(item.Id, Register6510.R13);
                    break;
                case "R14":
                    result.Add(item.Id, Register6510.R14);
                    break;
                case "R15":
                    result.Add(item.Id, Register6510.R15);
                    break;
                case "ACM":
                    result.Add(item.Id, Register6510.Acm);
                    break;
                case "YXM":
                    result.Add(item.Id, Register6510.Yxm);
                    break;
                case "LIN":
                    result.Add(item.Id, Register6510.Lin);
                    break;
                case "CYC":
                    result.Add(item.Id, Register6510.Cyc);
                    break;
                case "00":
                    result.Add(item.Id, Register6510.Zero);
                    break;
                case "01":
                    result.Add(item.Id, Register6510.One);
                    break;
                default:
                    _logger.Log(LogLevel.Warning, "Unknown available register {RegisterName}", item.Name);
                    break;
            }
        }
        _map = result.ToFrozenDictionary();
        _inverseMap = _map.ToFrozenDictionary(p => p.Value, p => p.Key);
    }
    public Registers6510 MapValues(RegistersResponse response)
    {
        Registers6510 result = Registers6510.Empty;
        foreach (var item in response.Items)
        {
            if (_map.TryGetValue(item.RegisterId, out var registerKey))
            {
                switch (registerKey)
                {
                    case Register6510.A:
                        result = result with { A = (byte)item.RegisterValue };
                        break;
                    case Register6510.X:
                        result = result with { X = (byte)item.RegisterValue };
                        break;
                    case Register6510.Y:
                        result = result with { Y = (byte)item.RegisterValue };
                        break;
                    case Register6510.PC:
                        result = result with { PC = item.RegisterValue };
                        break;
                    case Register6510.SP:
                        result = result with { SP = (byte)item.RegisterValue };
                        break;
                    case Register6510.Zero:
                        result = result with { Zero = (byte)item.RegisterValue };
                        break;
                    case Register6510.One:
                        result = result with { One = (byte)item.RegisterValue };
                        break;
                    case Register6510.Flags:
                        result = result with { Flags = (byte)item.RegisterValue };
                        break;
                    case Register6510.Lin:
                        result = result with { Lin = item.RegisterValue };
                        break;
                    case Register6510.Cyc:
                        result = result with { Cyc = item.RegisterValue };
                        break;
                    case Register6510.R3:
                        result = result with { R3 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R4:
                        result = result with { R4 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R5:
                        result = result with { R5 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R6:
                        result = result with { R6 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R7:
                        result = result with { R7 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R8:
                        result = result with { R8 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R9:
                        result = result with { R9 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R10:
                        result = result with { R10 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R11:
                        result = result with { R11 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R12:
                        result = result with { R12 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R13:
                        result = result with { R13 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R14:
                        result = result with { R14 = (byte)item.RegisterValue };
                        break;
                    case Register6510.R15:
                        result = result with { R15 = (byte)item.RegisterValue };
                        break;
                    case Register6510.Acm:
                        result = result with { Acm = (byte)item.RegisterValue };
                        break;
                    case Register6510.Yxm:
                        result = result with { Yxm = (byte)item.RegisterValue };
                        break;
                }
            }
            else
            {
                _logger.Log(LogLevel.Warning, "Register id {Id} can not be mapped", item.RegisterId);
            }
        }
        return result;
    }
    public byte? GetRegisterId(Register6510 register)
    {
        if (_inverseMap.TryGetValue(register, out var result))
        {
            return result;
        }
        return null;
    }
}
