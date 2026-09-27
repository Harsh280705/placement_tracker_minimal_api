# Placement Tracker Minimal API — SINGLE container (Vue + Minimal API + PostgreSQL 16 + NGINX).
#
# One image / one container serves the whole stack:
#   NGINX :8080  ->  /            serves the Vue production bundle (static files)
#                 ->  /api/...     proxies to the Minimal API on 127.0.0.1:5039
#   API           ->  PostgreSQL on 127.0.0.1:5432 (same PG16, NEW table
#                     minimal_api_applications; legacy Applications table untouched)
#   Data persists in the `pgdata_minimal_api` volume at /var/lib/postgresql/data.
#
# Port note: host port is 8081 (see docker-compose.yml) so this container runs
# side-by-side with the controller-based app on 8080. Internal NGINX port stays
# 8080; the Minimal API listens on 5039 internally (was 5038 in the old stack).
# Two processes cannot share one port in a single net namespace, hence 5039.

# ---------- Stage 1: build the Vue frontend (same sources, same build) ----------
FROM node:22-alpine AS frontend-build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ---------- Stage 2: publish the Minimal API ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY backend/*.csproj ./
RUN dotnet restore
COPY backend/ ./
RUN dotnet publish -c Release -o /app/publish

# ---------- Stage 3: single runtime (dotnet + nginx + postgres 16) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
WORKDIR /app

ENV DEBIAN_FRONTEND=noninteractive \
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://127.0.0.1:5039 \
    PGDATA=/var/lib/postgresql/data

# NGINX + PostgreSQL 16 (via the official PGDG repo: bookworm ships PG15,
# and we keep PG16 to match the previous postgres:16 service exactly).
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates gnupg wget \
 && wget -qO- https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor -o /usr/share/keyrings/pgdg.gpg \
 && echo "deb [signed-by=/usr/share/keyrings/pgdg.gpg] https://apt.postgresql.org/pub/repos/apt bookworm-pgdg main" > /etc/apt/sources.list.d/pgdg.list \
 && apt-get update \
 && apt-get install -y --no-install-recommends nginx postgresql-16 \
 && sed -i 's/^# *en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && locale-gen \
 && rm -f /etc/nginx/sites-enabled/default \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /var/run/postgresql /var/log/postgresql \
 && chown postgres:postgres /var/run/postgresql /var/log/postgresql

# Published Minimal API (from stage 2).
COPY --from=backend-build /app/publish/ /app/backend/
# Vue production bundle (from stage 1) served by NGINX.
COPY --from=frontend-build /app/dist/ /usr/share/nginx/html/
# NGINX single-container reverse proxy + static hosting.
COPY nginx/docker.conf /etc/nginx/conf.d/default.conf
# Entrypoint: init/starts postgres, starts the API, then runs NGINX.
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

EXPOSE 8080
VOLUME /var/lib/postgresql/data
ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
