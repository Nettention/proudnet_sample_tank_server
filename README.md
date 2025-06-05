# Tank Game Server Project

A ProudNet sample project implementing tank game server and client applications using C# and C++ with console interfaces.

## Project Overview

A real-time multiplayer tank game server/client implementation built with ProudNet networking library.

### Key Features

- **C# Server/Client**: .NET 9.0-based Windows console applications
- **C++ Server**: Cross-platform support (Windows/Linux) with Docker container support
- **PIDL Protocol**: Server-client RMI communication definitions
- **P2P Groups**: Efficient multiplayer networking
- **Real-time Gaming**: Tank movement, firing, and chat systems

### Project Structure

```
tank_server/
├── Common/           # PIDL protocol definitions and shared files
├── Server/           # C# server implementation
├── Client/           # C# client implementation
└── Server_CPP/       # C++ server implementation (Docker support)
```

### Game Features

- Real-time tank movement synchronization
- Shell firing and collision detection
- P2P messaging system
- Health management and respawn
- Server console commands (status, health, damage, heal, respawn)

## Documentation

For detailed installation, build, and execution instructions, refer to the documentation in the `docs/` folder:

- **[docs/01.server.en.md](docs/01.server.en.md)** - Comprehensive server project guide
- **[docs/01.server.en.html](docs/01.server.en.html)** - HTML version documentation


## Requirements

- **C# Version**: .NET 9.0, Visual Studio 2022
- **C++ Version**: CMake 3.10+, Visual Studio 2022, ATL Library
- **ProudNet**: Official ProudNet library installation
