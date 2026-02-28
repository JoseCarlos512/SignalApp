# Demo de Chat en Tiempo Real (Landing + Backoffice + Backend)

Este repositorio contiene un ejemplo **simple pero completo** para demostrar un flujo de chat en tiempo real con arquitectura separada:

- `landing-app` (Angular, sin login)
- `backoffice-app` (Angular, login JWT mock)
- `chat-backend` (.NET 8 + SignalR + JWT + SQL Server)
- `landing-backend` (.NET 8 + YARP, BFF/proxy para landing)

## Arquitectura

```text
landing-app ----REST/SignalR----> landing-backend ----REST/SignalR----> chat-backend <----REST/SignalR---- backoffice-app
```

### Backend principal (`chat-backend`)

Estructura simplificada:

- `Controllers/`
  - `AuthController`: login JWT mock para backoffice.
  - `ChatController`: endpoints REST de sesión, pendientes, estado asesor, tomar chat y mensajes.
- `Services/`
  - `InMemoryChatService`: persistencia en SQL Server de chats, postulantes, mensajes y estado de asesores.
- `Hubs/`
  - `ChatHub`: canal real-time de SignalR (grupos por chat + grupo asesores).
- `Models/`
  - modelos y DTOs.

### Backend landing (`landing-backend`)

Implementa un backend dedicado para landing:

- `LandingChatsController` expone endpoints propios para landing:
  - `POST /api/chats/session`
  - `GET /api/chats/{sessionId}`
  - `POST /api/chats/{sessionId}/messages`
- esos endpoints llaman a `chat-backend` vía `HttpClient` (base URL configurable).
- YARP se usa para enrutar `/hubs/chat` (SignalR / WebSocket) hacia `chat-backend`.

### Frontend Landing (`landing-app`)

- Botón **Hablar con un asesor**.
- Modal para datos básicos: Nombre, DNI, Teléfono, Email.
- Crea sesión de chat via REST.
- Muestra:
  - **Conectando con un asesor...** cuando hay asesores activos.
  - **No hay asesores disponibles en este momento** cuando no hay activos.
- Chat en tiempo real por SignalR.
- Consumirá su backend dedicado en `http://localhost:5100`.

### Frontend Backoffice (`backoffice-app`)

- Login JWT mock.
- Toggle **Estoy activo**.
- Bandeja de chats pendientes.
- Acción **Tomar chat**.
- Respuesta en tiempo real por SignalR.
- Consume `chat-backend` directamente en `http://localhost:5000`.

## Endpoints REST principales

### Landing backend (`http://localhost:5100`)

- `POST /api/chats/session`
- `GET /api/chats/{sessionId}`
- `POST /api/chats/{sessionId}/messages`
- Hub SignalR: `/hubs/chat`

### Backoffice / core backend (`http://localhost:5000`)

- `POST /api/auth/login`
- `GET /api/chats/pending` (JWT)
- `PATCH /api/chats/advisor/active` (JWT)
- `POST /api/chats/{sessionId}/take` (JWT)

## Cómo ejecutar

> Nota: este entorno no tiene `dotnet` ni acceso libre al registry de npm, pero el proyecto está listo para correr en tu máquina local.

### 1) Backend principal (chat-backend)

```bash
cd chat-backend
dotnet restore
dotnet run --urls http://localhost:5000
```

### 2) Backend landing (landing-backend)

```bash
cd landing-backend
dotnet restore
dotnet run --urls http://localhost:5100
```

### 3) Landing app

```bash
cd landing-app
npm install
npm start
```

Abrir: `http://localhost:4200`

### 4) Backoffice app

```bash
cd backoffice-app
npm install
npm start
```

Abrir: `http://localhost:4300`

## Flujo demo recomendado

1. Levantar `chat-backend` y `landing-backend`.
2. Abrir backoffice y hacer login con cualquier usuario/password (mock).
3. Marcar **Estoy activo**.
4. Abrir landing y hacer clic en **Hablar con un asesor**.
5. Completar datos, iniciar chat.
6. En backoffice, ver chat pendiente y **Tomar chat**.
7. Conversar en tiempo real entre landing y backoffice.

## Consideraciones de demo

- Persistencia en SQL Server (cadena `ConnectionStrings:ChatDatabase`) para chats, mensajes, postulantes y estado de asesores.
- JWT solo para endpoints de backoffice.
- Login mock intencional para simplificar.
- Diseño orientado a comprensión y adaptación rápida.
