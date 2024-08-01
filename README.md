# C64 Assembler Studio
C64 Assembler Studio is a cross-platform .NET based project aiming to create a comprehesive IDE including debugging experience for C64 assembler languages and [VICE emulator](https://vice-emu.sourceforge.io/) for debugging and running assembled programs. [Kick Assembler](https://www.theweb.dk/KickAssembler/) is first assembler supported. Others could follow. 

[Avalonia UI](https://docs.avaloniaui.net/) is used for GUI. For communication with VICE it is using my other project [VICE Binary Monitor Bridge for .NET](https://github.com/MihaMarkic/vice-bridge-net). A lot of code is same as in my  read-only debugging studio [Modern VICE PDB monitor](https://github.com/MihaMarkic/modern-vice-pdb-monitor).

## Status
Project is in development stage.

## Building from source
Currently this is the only way to test the application. A better solution will be provided soon.

Clone repository [retro-dbg-data-provider](https://github.com/MihaMarkic/retro-dbg-data-provider) into a sibling directory where this repository has been cloned. Open solution and build.

Note: Cloning of retro-dbg-data-provider repository is a temporary step. Eventually it will be removed and replaced with NuGet package.