using Dock.Model.Controls;
using Dock.Model.Core;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface IDockFactory: IFactory
{
    IRootDock? RootDock { get; }
}