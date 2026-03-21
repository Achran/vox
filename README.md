# Vox 🎙️

A Discord-like voice communication application built with .NET and Clean Architecture.

## Architecture Overview

Vox follows **Clean Architecture** principles with strict dependency rules (inner layers must not reference outer layers):

```
┌─────────────────────────────────────────────────────┐
│                    Clients                          │
│   ┌──────────────┐  ┌──────────┐  ┌──────────────┐ │
│   │  Vox.Web     │  │ Vox.Maui │  │ Vox.Shared.UI│ │
│   │ (Blazor WASM)│  │  (MAUI)  │  │  (Razor CL)  │ │
│   └──────────────┘  └──────────┘  └──────────────┘ │
├─────────────────────────────────────────────────────┤
│                    Presentation                     │
│   ┌─────────────────────────────────────────────┐   │
│   │           Vox.Api (ASP.NET Core)            │   │
│   └─────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────┤
│                   Infrastructure                    │
│   ┌─────────────────────────────────────────────┐   │
│   │  Vox.Infrastructure (EF Core, SignalR, ...)  │   │
│   └─────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────┤
│                      Core                           │
│   ┌────────────────┐  ┌───────────────────────┐     │
│   │ Vox.Application│  │     Vox.Domain        │     │
│   │ (MediatR, CQRS)│  │  (Entities, Repos)    │     │
│   └────────────────┘  └───────────────────────┘     │
└─────────────────────────────────────────────────────┘
```

**Dependency flow:** Domain ← Application ← Infrastructure ← API ← Clients

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend API** | ASP.NET Core 9 (Minimal API) |
| **Real-time** | SignalR |
| **Auth** | ASP.NET Identity + JWT |
| **Database** | PostgreSQL + Entity Framework Core 9 |
| **Shared UI** | Razor Class Library + MudBlazor |
| **Web Client** | Blazor WebAssembly (PWA) |
| **Desktop/Mobile** | .NET MAUI Blazor Hybrid |
| **CQRS** | MediatR |
| **Validation** | FluentValidation |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/) (for local development)
- [.NET MAUI workload](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation) (for building Vox.Maui)

## Project Structure

```
Vox.sln
├── src/
│   ├── Core/
│   │   ├── Vox.Domain/              # Entities, Value Objects, Repository interfaces
│   │   └── Vox.Application/         # Use Cases, MediatR commands/queries, DTOs
│   ├── Infrastructure/
│   │   └── Vox.Infrastructure/      # EF Core DbContext, SignalR Hubs, Repositories
│   ├── Presentation/
│   │   └── Vox.Api/                 # Minimal API endpoints, health-check, Swagger
│   └── Clients/
│       ├── Vox.Shared.UI/           # Shared Razor/Blazor components with MudBlazor
│       ├── Vox.Web/                 # Blazor WebAssembly PWA
│       └── Vox.Maui/               # .NET MAUI Blazor Hybrid (Windows, Android, iOS)
├── tests/
│   ├── Vox.Domain.Tests/
│   ├── Vox.Application.Tests/
│   ├── Vox.Infrastructure.Tests/
│   └── Vox.Api.Tests/
├── Directory.Build.props            # Shared MSBuild properties
├── Directory.Packages.props         # Central NuGet package management
├── global.json                      # .NET 9 SDK version pinning
├── .gitignore
└── .editorconfig
```

## Getting Started

### 1. Configure the database

Update the connection string in `src/Presentation/Vox.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=vox;Username=postgres;Password=yourpassword"
  }
}
```

### 2. Apply database migrations

```bash
dotnet ef database update --project src/Infrastructure/Vox.Infrastructure --startup-project src/Presentation/Vox.Api
```

### 3. Build the solution

```bash
dotnet build Vox.sln
```

> **Note:** `Vox.Maui` requires the MAUI workload (`dotnet workload install maui`) and is excluded from the default build on Linux.

### 4. Run the API

```bash
dotnet run --project src/Presentation/Vox.Api
```

The API will start at `https://localhost:5001`. OpenAPI docs are available at `/scalar/v1`.

### 5. Run the Web Client

```bash
dotnet run --project src/Clients/Vox.Web
```

### 6. Run Tests

```bash
dotnet test Vox.sln
```

## Health Check

The API exposes a health-check endpoint at `/health`.

## License

MIT
