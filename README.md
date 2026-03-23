# Vox 🎙️

A Discord-inspired voice and text communication application built with **.NET 9** and **Clean Architecture**.

## Architecture Overview

Vox follows Clean Architecture principles, ensuring that inner layers are independent of outer layers.

```
┌─────────────────────────────────────────────────────────────────┐
│                          Clients                                │
│  ┌──────────────────┐  ┌────────────┐  ┌─────────────────────┐ │
│  │   Vox.Web        │  │ Vox.Maui   │  │   Vox.Shared.UI     │ │
│  │ (Blazor WASM PWA)│  │(MAUI Hybrid│  │  (Razor Class Lib)  │ │
│  └────────┬─────────┘  └─────┬──────┘  └──────────┬──────────┘ │
│           └──────────────────┴─────────────────────┘            │
└──────────────────────────────┬──────────────────────────────────┘
                               │ HTTP / SignalR
┌──────────────────────────────▼──────────────────────────────────┐
│                    Presentation Layer                           │
│                        Vox.Api                                  │
│               (ASP.NET Core Minimal API)                        │
└──────────────────────────────┬──────────────────────────────────┘
                               │ Depends on
┌──────────────────────────────▼──────────────────────────────────┐
│                   Infrastructure Layer                          │
│                    Vox.Infrastructure                           │
│        (EF Core + PostgreSQL, SignalR Hubs, Identity)          │
└──────────────────────────────┬──────────────────────────────────┘
                               │ Depends on
┌──────────────────────────────▼──────────────────────────────────┐
│                    Application Layer                            │
│                    Vox.Application                              │
│            (MediatR CQRS, FluentValidation, DTOs)              │
└──────────────────────────────┬──────────────────────────────────┘
                               │ Depends on
┌──────────────────────────────▼──────────────────────────────────┐
│                      Domain Layer                               │
│                      Vox.Domain                                 │
│          (Entities, Value Objects, Domain Events,               │
│           Repository Interfaces – no external dependencies)     │
└─────────────────────────────────────────────────────────────────┘
```

### Clean Architecture Dependency Rules

- **Domain** → no dependencies (pure C# types only)
- **Application** → depends on Domain only
- **Infrastructure** → depends on Domain + Application
- **API** → depends on Application + Infrastructure
- **Clients** → communicate with API via HTTP/SignalR

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend API** | ASP.NET Core 9 (Minimal API) |
| **Real-time** | SignalR |
| **Auth** | ASP.NET Identity + JWT |
| **Database** | PostgreSQL + Entity Framework Core 9 |
| **Shared UI** | Razor Class Library (RCL) with MudBlazor |
| **Web Client** | Blazor WebAssembly (PWA) |
| **Desktop/Mobile Client** | .NET MAUI Blazor Hybrid (Windows, Android, iOS) |
| **CQRS** | MediatR |
| **Validation** | FluentValidation |
| **Testing** | xUnit |

## Solution Structure

```
Vox.sln
├── src/
│   ├── Core/
│   │   ├── Vox.Domain/               # Entities, Value Objects, Domain Events, Repository interfaces
│   │   └── Vox.Application/          # Use Cases, MediatR CQRS, FluentValidation, DTOs
│   ├── Infrastructure/
│   │   └── Vox.Infrastructure/       # EF Core DbContext, Repositories, SignalR Hubs, Identity
│   ├── Presentation/
│   │   └── Vox.Api/                  # Minimal API endpoints, health-check, SignalR mapping
│   └── Clients/
│       ├── Vox.Shared.UI/            # Shared Blazor components + MudBlazor (RCL)
│       ├── Vox.Web/                  # Blazor WebAssembly (PWA) – web + Linux
│       └── Vox.Maui/                 # .NET MAUI Blazor Hybrid – Windows, Android, iOS
├── tests/
│   ├── Vox.Domain.Tests/             # Domain entity / logic unit tests
│   ├── Vox.Application.Tests/        # Command/query handler + validator tests
│   ├── Vox.Infrastructure.Tests/     # Repository + DbContext tests (InMemory)
│   └── Vox.Api.Tests/                # API integration test stubs
├── .editorconfig                     # C# code style conventions
├── .gitignore                        # .NET gitignore
├── Directory.Build.props             # Shared MSBuild properties (nullable, implicit usings)
├── Directory.Packages.props          # Central NuGet package version management
├── global.json                       # .NET 9 SDK version pinning
└── README.md
```

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 9.x | See `global.json` |
| PostgreSQL | 14+ | For API and Infrastructure |
| Docker & Docker Compose | Latest | For running PostgreSQL, LiveKit, and Redis locally |
| VS Code or Visual Studio | Latest | Recommended with C# Dev Kit |
| MAUI Workload | Latest | Required only for `Vox.Maui` |

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Achran/vox.git
cd vox
```

### 2. Install .NET workloads (optional – required for MAUI project)

```bash
dotnet workload install maui
```

### 3. Start infrastructure services (PostgreSQL, LiveKit, Redis)

Using Docker Compose (recommended):

```bash
docker compose up -d
```

This starts:
- **PostgreSQL 16** on port `5432`
- **Redis 7** on port `6379`
- **LiveKit Server** on ports `7880` (HTTP), `7881` (RTC), `7882/udp` (WebRTC)

LiveKit is pre-configured with API key `devkey` and secret `secret` (see `livekit.yaml`).

Alternatively, start only PostgreSQL:

```bash
docker run -d \
  --name vox-postgres \
  -e POSTGRES_DB=vox \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16
```

### 4. Configure the API

Edit `src/Presentation/Vox.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=vox;Username=postgres;Password=postgres"
  }
}
```

### 5. Apply database migrations

```bash
cd src/Presentation/Vox.Api
dotnet ef database update --project ../../Infrastructure/Vox.Infrastructure
```

### 6. Build the solution

```bash
dotnet build Vox.sln
```

> **Note:** Building the MAUI project (`Vox.Maui`) requires the `maui` workload.
> The default solution build excludes `Vox.Maui` to allow building without the workload.
> To build MAUI: `dotnet build src/Clients/Vox.Maui/Vox.Maui.csproj`

### 7. Run the API

```bash
dotnet run --project src/Presentation/Vox.Api
```

API will be available at `https://localhost:7001` (or `http://localhost:5001`).

### 8. Run the Web client

```bash
dotnet run --project src/Clients/Vox.Web
```

### 9. Run tests

```bash
dotnet test Vox.sln
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check |
| `WS` | `/hubs/chat` | SignalR chat hub |
| `WS` | `/hubs/voice` | SignalR voice hub |
| `GET` | `/api/voice/token/{channelId}` | Get LiveKit access token |
| `POST` | `/api/voice/rooms/{channelId}` | Create a LiveKit voice room |
| `DELETE` | `/api/voice/rooms/{channelId}` | Delete a LiveKit voice room |

## Development Notes

- **Central Package Management** – all NuGet versions are declared in `Directory.Packages.props`
- **Clean Architecture enforcement** – domain and application layers have no infrastructure references
- **MudBlazor** – used in `Vox.Shared.UI` for a Material Design UI, shared by both web and MAUI clients
- **SignalR** – used for real-time text chat; future: WebRTC signaling for voice

## Contributing

1. Follow the Clean Architecture dependency rules
2. Add unit tests for all new domain logic and application handlers
3. Follow the conventions in `.editorconfig`
