Placement Tracker — .NET Minimal API

A Placement Tracker backend built using C# and ASP.NET Core Minimal APIs with PostgreSQL and Entity Framework Core.

Tech Stack
Backend: C# + ASP.NET Core Minimal API
ORM: Entity Framework Core
Database: PostgreSQL
Frontend: Vue 3
Reverse Proxy: NGINX
Containerization: Docker
Architecture
Vue 3 Frontend
      ↓
    NGINX
      ↓
ASP.NET Core Minimal API
      ↓
 Entity Framework Core
      ↓
  PostgreSQL
API Endpoints
Method	Endpoint	Purpose
GET	/api/applications	Get all applications
GET	/api/applications/{id}	Get application by ID
POST	/api/applications	Create application
PUT	/api/applications/{id}	Update application
DELETE	/api/applications/{id}	Delete application
Key Difference

Unlike the controller-based .NET version, this implementation uses Minimal API endpoint mapping with MapGet, MapPost, MapPut, and MapDelete instead of ControllerBase and controller action methods.

The Minimal API uses a separate PostgreSQL table, minimal_api_applications, so it does not interfere with the original implementation.
