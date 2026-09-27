# Placement Tracker — .NET Minimal API

A backend implementation of the **Placement Tracker** built using **C# and ASP.NET Core Minimal APIs**. It provides CRUD operations for placement applications and uses PostgreSQL for persistent storage.

## Tech Stack

* **Language:** C#
* **Backend:** ASP.NET Core Minimal API
* **ORM:** Entity Framework Core
* **Database:** PostgreSQL
* **Frontend:** Vue 3
* **Reverse Proxy:** NGINX
* **Containerization:** Docker
* **Testing:** xUnit

## Architecture

```text
Vue 3 Frontend
       ↓
     NGINX
       ↓
ASP.NET Core Minimal API
       ↓
Entity Framework Core
       ↓
   PostgreSQL
```

## API Endpoints

| Method   | Endpoint                 | Description              |
| -------- | ------------------------ | ------------------------ |
| `GET`    | `/api/applications`      | Get all applications     |
| `GET`    | `/api/applications/{id}` | Get an application by ID |
| `POST`   | `/api/applications`      | Create a new application |
| `PUT`    | `/api/applications/{id}` | Update an application    |
| `DELETE` | `/api/applications/{id}` | Delete an application    |

## Key Features

* CRUD operations for placement applications
* Manual request validation
* DTO-based request/response handling
* Entity Framework Core database access
* PostgreSQL persistence
* REST API endpoints using Minimal API
* Vue 3 frontend
* Docker-based deployment
* NGINX reverse proxy
* Automated backend tests

## Database

The Minimal API uses a separate PostgreSQL table:

```text
minimal_api_applications
```

This keeps the Minimal API implementation isolated from the original controller-based .NET implementation.

## Minimal API vs Controller API

The main architectural difference is that the original .NET backend uses **Controllers**, while this implementation uses **Minimal API endpoint mapping**:

```text
Controller API:
Request → Controller → Action → EF Core → PostgreSQL

Minimal API:
Request → Endpoint → EF Core → PostgreSQL
```

The core CRUD functionality, validation, database access, and frontend contract remain largely the same.
