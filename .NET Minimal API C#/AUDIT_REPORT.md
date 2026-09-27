# AUDIT REPORT — Controller-Based Web API vs Minimal API

**Date:** 2026-09-25
**Source (read-only):** `D:\Yantra3loka LLP\Training\Week 1\Placement Tracker\.NET\Placement Tracker - .NET + Vue + Postgre + Docker`
**New:** `D:\Yantra3loka LLP\Training\Week 1\Placement Tracker\.NET Minimal API C#`
**Method:** read-only comparison of actual files; behaviors below were verified by
`dotnet test` (26/26 pass), `docker compose build/up`, live CRUD via
`http://localhost:8081`, `psql` table inspection, and a container restart test.

## Comparison table

| Area | Existing .NET Web API | New Minimal API |
|------|------------------------|-----------------|
| Backend framework | ASP.NET Core 8 Web API, controller-based | ASP.NET Core 8 Minimal API |
| Endpoint style | `ApplicationsController : ControllerBase` with `[ApiController]`, `[Route("api/applications")]`, `[HttpGet]/[HttpPost]/[HttpPut]/[HttpDelete]` | `Endpoints/ApplicationEndpoints.cs` with `MapGroup("/api/applications")` + `MapGet/MapPost/MapPut/MapDelete`; no controller types exist (guarded by test) |
| Controllers | `backend/Controllers/ApplicationsController.cs` (115 lines) | None — folder does not exist |
| Routing | `app.MapControllers()` + attribute routes | `app.MapApplicationEndpoints()` (RouteGroupBuilder) |
| Program.cs | `AddControllers()`, `MapControllers()`, no `AddAuthorization` | `AddEndpointsApiExplorer/SwaggerGen/AddAuthorization`, `UseAuthorization`, `MapApplicationEndpoints()`; CORS origins retargeted to `:5174`/`:8081` |
| Dependency injection | Constructor injection (`ApplicationsController(ApplicationDbContext db)`) | Parameter injection in lambdas (`(ApplicationDbContext db) => ...`) |
| Model/entity | `Models/Application.cs` (IValidatableObject, same rules) | `Models/Application.cs` — same fields/rules, new namespace `PlacementTracker.MinimalApi` |
| Database | PostgreSQL 16 | Same PostgreSQL 16 system/environment |
| Table | `Applications` (EF default) — CRUD reads/writes it | `minimal_api_applications` (`ToTable`, `ApplicationDbContext.TableName`) — CRUD only here; legacy table never touched (verified: only `minimal_api_applications` exists in new volume) |
| ORM | EF Core + Npgsql, `timestamp without time zone` columns, `EnableLegacyTimestampBehavior`, `EnsureCreated` + dev seed-when-empty | Identical EF Core setup; only table name differs; seed guard prevents duplicates (verified: 3 rows before and after restart) |
| Validation | `[ApiController]` auto-ModelState → `ValidationProblem` | Manual `ValidateDto()` (DataAnnotations + `Results.ValidationProblem`, same error shape); same rules: company/role 2–100, status in {Wishlist, Applied, Interview, Offer, Rejected}, AppliedOn required unless Wishlist, valid URL, notes ≤ 1000 |
| Error handling | `404 { message }`, `ValidationProblem` 400 | Identical: `Results.NotFound(new { message })`, `Results.ValidationProblem(errors)` |
| HTTP responses | 200/200/201+Location/200/204 | Identical codes; POST returns `Results.Created("/api/applications/{id}", dto)` |
| Frontend | Vue 3, `/api/applications` via relative path, dev `:5173` proxy → `:5038` | Same Vue sources/UI (`applicationService.js` byte-identical); only `vite.config.js` retargeted (dev `:5174` proxy → `:5039`) |
| NGINX | `docker.conf` proxies `/api/` → `127.0.0.1:5038`, public `:8080` | Same structure, proxies `/api/` → `127.0.0.1:5039`, public host `:8081` |
| Container | `app` service, host `8080:8080`, volume `pgdata` | `minimal-api-app` service, `container_name: placement-tracker-minimal-api`, host `8081:8080`, volume `pgdata_minimal_api`; single-container concept (NGINX+API+PG16+Vue dist) preserved |
| Testing | `ApplicationsApiTests` (controller unit) + `IntegrationTests` + `ValidationTests` | `IntegrationTests` (same HTTP cases) + `ValidationTests` (same rules) + `MinimalApiTests` (table mapping, no-controller guard, Map* presence, ValidateDto 400, InMemory CRUD) — 26/26 pass |

## Detailed findings

1. **Architecture comparison** — controller pipeline replaced by Minimal API
   route-group lambdas; namespaces renamed `PlacementTracker.Api` →
   `PlacementTracker.MinimalApi`; no shared code/binaries between projects.
2. **Endpoint implementation comparison** — same 5 routes, same ordering
   (`OrderByDescending(Id)`), same trim/`NormalizeOptional` handling.
3. **Controller vs Minimal API difference** — see table; verified by source
   search test (`Backend_uses_no_controller_infrastructure`).
4. **Program.cs difference** — no `AddControllers`/`MapControllers`;
   `AddAuthorization` added (fixes `UseAuthorization` startup requirement);
   CORS origins changed to `:5174`/`:8081`.
5. **Routing difference** — attributes → `MapGroup`/`Map{Get,Post,Put,Delete}`.
6. **Dependency injection difference** — constructor → lambda parameters.
7. **Model/entity difference** — fields, attributes, `IValidatableObject`
   rules identical; only namespace differs.
8. **Database table difference** — `Applications` vs `minimal_api_applications`;
   `psql` confirmed only the new table exists in the new container.
9. **EF Core usage** — same provider, column types, timestamp switch,
   `EnsureCreated`, dev-only seed-when-empty.
10. **Validation difference** — manual `ValidateDto` instead of automatic
    ModelState; same rules and 400 shape (verified live: invalid POST → 400).
11. **Error handling difference** — none functionally (same 404 message body).
12. **HTTP response handling** — `Ok/CreatedAtAction/NoContent` →
    `Results.Ok/Created/NoContent`; status codes identical (verified live).
13. **Testing difference** — controller unit tests replaced by Minimal API
    architecture/table/validation tests; HTTP + validation suites preserved.
14. **Docker/container difference** — separate image/container name, host port,
    volume; same single-container topology and healthcheck path.
15. **NGINX difference** — proxy target port only (`5038` → `5039`).
16. **Frontend difference** — UI/components/routes/services unchanged
    (`applicationService.js` diff-empty); only dev-server port/proxy target.
17. **PostgreSQL difference** — same version/env; isolated data via new volume
    and new table.
18. **Files/folders added** — `backend/Endpoints/ApplicationEndpoints.cs`,
    `backend/Properties/launchSettings.json`, `tests/MinimalApiTests.cs`,
    `README.md`, `AUDIT_REPORT.md`, Docker/NGINX configs, copied `frontend/`.
19. **Files/folders removed** — N/A (greenfield folder); relative to source,
    `Controllers/` and `Views`/`wwwroot` were intentionally not carried over.
20. **Files intentionally kept the same** — frontend sources (except
    `vite.config.js` ports), model/DTO rules, entrypoint logic, Dockerfile
    structure, README structure.
21. **API contract comparison** — method/URL/request/response/status/error
    identical; verified live (GET all → 200 array of 3; POST → 201; GET one →
    200; PUT → 200; DELETE → 204; invalid → 400; missing → 404).
22. **What functionality remained unchanged** — all CRUD behavior, validation
    rules, JSON shapes, seed content, SPA hosting, healthcheck.
23. **What changed specifically because of Minimal API** — endpoint definition
    style, validation plumbing, DI style, `Program.cs` wiring; plus
    side-by-side ports/table/container required for independent coexistence.
24. **Verification results** —
    - `dotnet build`: success, 0 warnings, 0 errors.
    - `dotnet test`: 26/26 passed.
    - `docker compose build`: success (after changing publish to restore
      implicitly; the layered `--no-restore` failed on a stale restore cache
      with NETSDK1064).
    - `docker compose up`: healthy; `GET /api/applications` → 200 (3 seeds,
      SQL log shows `FROM minimal_api_applications`).
    - Live CRUD: POST → 201 (id 4), GET one → 200, PUT → 200, DELETE → 204,
      count back to 3; invalid POST → 400; missing GET → 404; `/` → 200 Vue app.
    - Restart: count still 3 (persistence + idempotent seed confirmed).
    - Source project: `git status` clean — never modified.

## Final verification checklist

- [x] Source .NET project was only READ, not modified.
- [x] New implementation exists ONLY in `.NET Minimal API C#`.
- [x] Backend is genuinely ASP.NET Core Minimal API.
- [x] No ControllerBase-based API remains in the new backend.
- [x] Endpoints use MapGet/MapPost/MapPut/MapDelete.
- [x] Vue frontend remains functionally the same.
- [x] PostgreSQL is used.
- [x] Minimal API uses a DIFFERENT database table (`minimal_api_applications`).
- [x] Existing .NET table remains untouched (separate volume; not referenced).
- [x] Minimal API has a DIFFERENT Docker container (`placement-tracker-minimal-api`).
- [x] Existing .NET container is not reused.
- [x] NGINX configuration works (verified `/` + `/api/` live).
- [x] API works. [x] Frontend works. [x] CRUD works. [x] Validation works.
- [x] Tests pass (26/26). [x] Docker build succeeds. [x] Docker startup succeeds.
- [x] Data persists after restart. [x] README updated. [x] AUDIT_REPORT.md created.
