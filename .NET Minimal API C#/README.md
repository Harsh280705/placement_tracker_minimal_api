# Placement Tracker — ASP.NET Core Minimal API edition (Vue 3 + Minimal API + PostgreSQL + NGINX)

A complete migration of the Placement Tracker backend from ASP.NET Core
Controller-Based Web API to ASP.NET Core Minimal API. The Vue 3 frontend,
PostgreSQL environment, and single-container Docker concept are preserved;
the API runs in its OWN container on its OWN table.

## Tech Stack

- Frontend: Vue 3 + Vite + Vue Router (same sources as the controller version)
- Backend: ASP.NET Core 8 Minimal API (no controllers)
- Database: PostgreSQL 16
- ORM: Entity Framework Core + Npgsql
- Reverse Proxy: NGINX
- Testing: xUnit + Moq + EF InMemory + WebApplicationFactory

## Architecture

```text
Browser
   ↓
NGINX :8081 (host) / :8080 (container)
   ├── /       → Vue production bundle (static files)
   └── /api/   → Minimal API on 127.0.0.1:5039 (internal)
                          ↓
                     PostgreSQL :5432 (internal)
                     table: minimal_api_applications
```

Port note: the controller-based app uses host ports 8080 / 5038 / 5173.
This edition uses **8081 / 5039 / 5174** so both stacks run side-by-side.

## Project Structure

```text
backend/
  Program.cs               → Minimal API host (no AddControllers/MapControllers)
  Endpoints/
    ApplicationEndpoints.cs → MapGet/MapPost/MapPut/MapDelete for /api/applications
  Data/
    ApplicationDbContext.cs → EF Core, mapped to "minimal_api_applications"
  Models/
    Application.cs          → entity + validation (same rules as before)
  DTOs/
    ApplicationDtos.cs      → Create/Update/Response DTOs (same rules as before)
  appsettings.json / appsettings.Development.json
  Properties/launchSettings.json (http://localhost:5039)
frontend/                  → Vue 3 app (same UI; only vite ports/proxy retargeted)
nginx/docker.conf          → NGINX proxy to 127.0.0.1:5039
tests/                     → validation, Minimal API, and HTTP integration tests
Dockerfile                 → single image: NGINX + Minimal API + PostgreSQL 16 + Vue dist
docker-compose.yml         → service minimal-api-app → http://localhost:8081
docker-entrypoint.sh       → inits/starts postgres, API (:5039), then NGINX
```

## Database

- Same PostgreSQL 16 system/environment as the existing project.
- The Minimal API uses a DIFFERENT table: **`minimal_api_applications`**.
- The legacy `Applications` table is never read or written by this app.
- Initialization is idempotent (`EnsureCreated` + seed-only-when-empty), so
  restarts never duplicate data.

## Requirements

* .NET 8 SDK
* Node.js + npm
* Docker (for container run)
* PostgreSQL 16 (only for local non-Docker run)

## Run Locally

### 1. Start Backend

```powershell
dotnet run --project backend/PlacementTracker.MinimalApi.csproj
```

API:

```text
http://localhost:5039/api/applications
```

(requires PostgreSQL on 127.0.0.1:5432, database `placement_tracker`)

### 2. Start Frontend

```powershell
cd frontend
npm install
npm run dev
```

Vue:

```text
http://localhost:5174
```

(`/api` is proxied to `http://localhost:5039`.)

## Run Tests

```powershell
dotnet test tests/PlacementTracker.MinimalApi.Tests.csproj
```

26 tests, all passing: HTTP integration (GET/POST/PUT/DELETE, 400/404),
validation rules, table mapping, no-controller-architecture guard.
Tests use isolated InMemory databases and never touch PostgreSQL.

## Run with Docker

```powershell
docker compose up --build
```

Application:

```text
http://localhost:8081
```

Stop (keeps data volume):

```powershell
docker compose down
```

Public access:

```powershell
ngrok http 8081
```

**Important:** Expose port `8081` through ngrok, not `5174` or `5039`.

## API Endpoints

| Method | Endpoint                 | Success | Errors            |
| ------ | ------------------------ | ------- | ----------------- |
| GET    | `/api/applications`      | 200     | —                 |
| GET    | `/api/applications/{id}` | 200     | 404               |
| POST   | `/api/applications`      | 201     | 400 (validation)  |
| PUT    | `/api/applications/{id}` | 200     | 400 / 404         |
| DELETE | `/api/applications/{id}` | 204     | 404               |

Request/response JSON is identical to the controller version
(camelCase: `company`, `role`, `status`, `appliedOn`, `jobUrl`, `notes`,
`createdAt`). Validation errors return `400 ValidationProblem` (same shape);
missing records return `404 { "message": "Application {id} not found." }`.

## Ports

| Service          |             Port |
| ---------------- | ---------------: |
| PostgreSQL       |             5432 |
| Minimal API      |             5039 |
| Vue (dev)        |             5174 |
| NGINX            |  8081 (host)     |
| ngrok            | Public HTTPS URL |

## How this differs from the controller-based version

See [AUDIT_REPORT.md](AUDIT_REPORT.md) for the full comparison.
In short: `ApplicationsController` (ControllerBase + attributes) was replaced
by `Endpoints/ApplicationEndpoints.cs` (`MapGroup` + `MapGet/MapPost/MapPut/
MapDelete`); `Program.cs` no longer calls `AddControllers`/`MapControllers`;
validation is applied manually via `ValidateDto`; EF Core maps to the new
`minimal_api_applications` table; Docker uses a separate container
(`placement-tracker-minimal-api`), host port, and volume.
