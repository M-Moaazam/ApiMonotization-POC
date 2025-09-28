# API Monetization Gateway - .NET POC

## Prerequisites
- .NET 7 SDK
- Docker & docker-compose

## Run with Docker (recommended)
1. Build & run:
   ```bash
   docker-compose up --build```
   
2. Wait for SQL Server/Redis to be ready. The API will seed demo data and log a demo API key on startup.
3. Call the health endpoint:
   ```bash
   curl -H "x-api-key: <the-seeded-key-from-logs>" http://localhost:5000/health```
   
## Run locally (without Docker)
1. Set connection strings in Gateway.Api/appsettings.json.
2. Apply migrations:
   ```bash
   dotnet ef database update --project Gateway.Infrastructure --startup-project Gateway.Api```
3. Run:
   ```bash
   dotnet run --project Gateway.Api```
   
*Notes*

* Rate limiting uses Redis if Redis:Connection is set; otherwise, falls back to in-memory rate limiter.

* Monthly summarizer runs daily and writes summaries into MonthlySummaries table.


---

# Final checklist & tips

1. **Controllers**: Add a simple `HealthController` (shown earlier) and any test endpoints you want to protect behind the gateway.
2. **Order of middleware**: Put `UseApiKeyAuth()` before `UseApiGateway()` and both before MVC controllers.
3. **Secrets**: Use environment variables for DB password / redis in production.
4. **Redis in Docker**: `Redis__Connection` should match the compose service name `redis:6379`.
5. **Migrations in Docker**: You can auto-run migrations at startup via `DataSeeder` which calls `db.Database.MigrateAsync()`. Ensure your DB user has permission.
6. **Testing**: Integration tests use `UseInMemoryDatabase` to avoid external dependence. For end-to-end tests against dockerized SQL+Redis use testcontainers or a CI job.
