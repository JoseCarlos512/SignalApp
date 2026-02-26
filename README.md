# Demo de Chat en Tiempo Real (Landing + Backoffice + Backend)

Este repositorio contiene un ejemplo **simple pero completo** para demostrar un flujo de chat en tiempo real con arquitectura separada:

- `landing-app` (Angular, sin login)
- `backoffice-app` (Angular, login JWT mock)
- `chat-backend` (.NET 8 + SignalR + JWT + memoria)

## Arquitectura

```text
landing-app ----REST/SignalR----> chat-backend <----REST/SignalR---- backoffice-app
```

### Backend (`chat-backend`)

Estructura simplificada:

- `Controllers/`
  - `AuthController`: login JWT mock para backoffice.
  - `ChatController`: endpoints REST de sesión, pendientes, estado asesor, tomar chat y mensajes.
- `Services/`
  - `InMemoryChatService`: estado en memoria de chats y asesores.
- `Hubs/`
  - `ChatHub`: canal real-time de SignalR (grupos por chat + grupo asesores).
- `Models/`
  - modelos y DTOs.

### Frontend Landing (`landing-app`)

- Botón **Hablar con un asesor**.
- Modal para datos básicos: Nombre, DNI, Teléfono, Email.
- Crea sesión de chat via REST.
- Muestra:
  - **Conectando con un asesor...** cuando hay asesores activos.
  - **No hay asesores disponibles en este momento** cuando no hay activos.
- Chat en tiempo real por SignalR.

### Frontend Backoffice (`backoffice-app`)

- Login JWT mock.
- Toggle **Estoy activo**.
- Bandeja de chats pendientes.
- Acción **Tomar chat**.
- Respuesta en tiempo real por SignalR.

## Endpoints REST principales

Base URL backend: `http://localhost:5000`

- `POST /api/auth/login`
- `POST /api/chats/session`
- `GET /api/chats/pending` (JWT)
- `PATCH /api/chats/advisor/active` (JWT)
- `POST /api/chats/{sessionId}/take` (JWT)
- `POST /api/chats/{sessionId}/messages`
- `GET /api/chats/{sessionId}`

Hub SignalR: `/hubs/chat`

## Cómo ejecutar

> Nota: este entorno no tiene `dotnet` ni acceso libre al registry de npm, pero el proyecto está listo para correr en tu máquina local.

### 1) Backend

```bash
cd chat-backend
dotnet restore
dotnet run --urls http://localhost:5000
```

### 2) Landing app

```bash
cd landing-app
npm install
npm start
```

Abrir: `http://localhost:4200`

### 3) Backoffice app

```bash
cd backoffice-app
npm install
npm start
```

Abrir: `http://localhost:4300`

## Flujo demo recomendado

1. Abrir backoffice y hacer login con cualquier usuario/password (mock).
2. Marcar **Estoy activo**.
3. Abrir landing y hacer clic en **Hablar con un asesor**.
4. Completar datos, iniciar chat.
5. En backoffice, ver chat pendiente y **Tomar chat**.
6. Conversar en tiempo real entre landing y backoffice.

## Consideraciones de demo

- Persistencia en memoria (se pierde al reiniciar backend).
- JWT solo para endpoints de backoffice.
- Login mock intencional para simplificar.
- Diseño orientado a comprensión y adaptación rápida.
