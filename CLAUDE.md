# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A real-time customer service chat system with:
- **chat-backend**: .NET 8 core chat service with clean architecture (SignalR + EF Core + SQL Server)
- **landing-backend**: .NET 8 reverse proxy/BFF using YARP that proxies landing-app traffic to chat-backend
- **landing-app**: Angular 19 public-facing app with multi-step postulation form and chat widget
- **backoffice-app**: Angular 19 admin app for advisors to manage chats

## Build & Run Commands

### .NET Backends
```bash
# chat-backend — build solución completa
cd chat-backend && dotnet build ChatBackend.sln

# chat-backend — ejecutar API (puerto 5000)
cd chat-backend/ChatBackend.Api && dotnet run --urls http://localhost:5000

# landing-backend (YARP proxy, port 5100)
cd landing-backend && dotnet restore && dotnet run --urls http://localhost:5100
```

### Angular Frontends
```bash
# landing-app (port 4200)
cd landing-app && npm install && npm start

# backoffice-app (port 4300)
cd backoffice-app && npm install && npm start

# Production builds
npm run build
```

### Database Migrations (EF Core)
```bash
# El DbContext está en Infrastructure; el startup project es Api
cd chat-backend
dotnet ef database update --project ChatBackend.Infrastructure --startup-project ChatBackend.Api
dotnet ef migrations add <NombreMigracion> --project ChatBackend.Infrastructure --startup-project ChatBackend.Api
```

No test runner or linter is currently configured in any project.

## Architecture

### Request Flow
```
landing-app (4200)
    → landing-backend (5100) [YARP reverse proxy]
        → chat-backend (5000) [REST + SignalR]
            → SQL Server (SignalChatDb)

backoffice-app (4300)
    → chat-backend (5000) directly [JWT-authenticated REST + SignalR]
```

### chat-backend: Clean Architecture (4 proyectos)

```
chat-backend/
├── ChatBackend.sln
├── ChatBackend.Domain/           (class library — sin dependencias externas)
│   ├── Entities/                 # ChatSession, ChatMessage, AdvisorState
│   ├── Enums/                    # ChatStatus (Pending | Assigned | Closed)
│   ├── Interfaces/               # IChatService, IChatRepository, IAdvisorRepository, IAdvisorConnectionManager
│   └── ValueObjects/             # ApplicantInfo record
├── ChatBackend.Application/      (class library → Domain)
│   └── Services/                 # ChatService — orquesta lógica de negocio
├── ChatBackend.Infrastructure/   (class library → Domain + EF Core + SqlServer)
│   ├── Migrations/               # EF Core migrations
│   ├── Persistence/              # ChatDbContext, EfChatRepository, EfAdvisorRepository, ChatEntities
│   └── Realtime/                 # InMemoryAdvisorConnectionManager (ConcurrentDictionary)
└── ChatBackend.Api/              (ASP.NET Core Web → Application + Infrastructure + JWT)
    ├── Controllers/              # ChatController (api/chats), AuthController (api/auth)
    ├── Hubs/                     # ChatHub — SignalR
    ├── Models/                   # Request/Response DTOs
    └── Program.cs
```

**Referencias entre proyectos**:
- `Domain` ← `Application`
- `Domain` ← `Infrastructure`
- `Application` + `Infrastructure` ← `Api`

### Real-Time Communication (SignalR)
`ChatHub` manages WebSocket groups:
- `advisors` — all online advisors
- `advisor-{advisorId}` — individual advisor
- `chat-{sessionId}` — participants of a specific chat

JWT is passed via query string for WebSocket auth: `?access_token=...`

On connect, the hub calls `IAdvisorConnectionManager.AddConnection` (in-memory) and `IAdvisorRepository.EnsureAdvisorExists` (DB).

### Key Design Decisions
- **`IChatRepository`** receives the system message text as a parameter — business logic for message composition stays in `ChatService`, not in the repository.
- **`IAdvisorConnectionManager`** is registered as **Singleton** (shared in-memory state). All other services are **Scoped**.
- **`ExecuteUpdate`** is used for atomic status transitions (`TakeChat`, `TransferChat`, `CloseChat`) to prevent race conditions — then a separate `SaveChanges` inserts the system message.
- The `CloseChat` in `ChatService` constructs the system message from `closedBy` + `reason` before calling the repository, keeping the repository free of business text.

### Authentication
JWT mock — any username/password generates a valid token. Config in appsettings.json:
```
Key: "DemoSuperSecretKeyForJwtOnly123!"
Issuer: "chat-backend"
Audience: "chat-backoffice"
```

### Database
SQL Server connection string (appsettings.json):
```
Server=DESKTOP-2I5MGSR\SQLEXPRESS;Database=SignalChatDb;User Id=sa;Password=123456;TrustServerCertificate=True
```

### YARP Routing (landing-backend)
| Route | Target |
|---|---|
| `POST /api/chats/session` | chat-backend |
| `GET /api/chats/{sessionId:guid}` | chat-backend |
| `POST /api/chats/{id}/messages` | chat-backend |
| `WS /hubs/chat/**` | chat-backend |

### Frontend Angular Apps
Both apps use standalone components (no NgModules) and `@microsoft/signalr` for real-time events. CORS is AllowAll in backends (development only).
