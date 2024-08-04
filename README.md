# C64 Assembler Studio
C64 Assembler Studio is a cross-platform .NET based project aiming to create a comprehesive IDE including debugging experience for C64 assembler languages and [VICE emulator](https://vice-emu.sourceforge.io/) for debugging and running assembled programs. [Kick Assembler](https://www.theweb.dk/KickAssembler/) is first assembler supported. Others could follow. 

[Avalonia UI](https://docs.avaloniaui.net/) is used for GUI. For communication with VICE it is using my other project [VICE Binary Monitor Bridge for .NET](https://github.com/MihaMarkic/vice-bridge-net). A lot of code is same as in my  read-only debugging studio [Modern VICE PDB monitor](https://github.com/MihaMarkic/modern-vice-pdb-monitor).

## Status
Project is in early preview stage.

## Quick Start

See [Docs](Docs/quick-start.md).

## Prerequisites
Java should be installed and in the CLI path, so it is found by typing java in the command line.

VICE files should be on disk, and it should have enabled binary monitor (at Preferences/Settings, Host/Monitor Enable binary monitor should be checked and pointing to ip4://127.0.0.1:6502).

## Building from source
For the latest binaries, check [Releases](Releases).

Clone repository [retro-dbg-data-provider](https://github.com/MihaMarkic/retro-dbg-data-provider) into a sibling directory where this repository has been cloned. Open solution and build.

Note: Cloning of retro-dbg-data-provider repository is a temporary step. Eventually it will be removed and replaced with NuGet package.