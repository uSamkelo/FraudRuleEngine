# FraudRuleEngine

A transaction fraud-detection service. It ingests categorized transaction events, runs each one through a set of pluggable fraud rules, persists both the transaction and any resulting alerts in PostgreSQL, and exposes everything through a REST API.

- **Categorized transaction events** — every transaction carries a category (`Payment`/`Withdrawal`/`Transfer`/`Deposit`), channel (`Online`/`ATM`/`POS`/`Branch`), currency, country, merchant/MCC, device, and card metadata.
- **Fraud rules per transaction, on different criteria** — 7 independent rules covering amount, velocity, transaction rate, geography, time-of-day + risk tier, merchant category, and account age.
- **Stored in a data store** — PostgreSQL via EF Core, with schema migrations applied automatically at startup.
- **Retrieval via an API** — a REST API to submit transactions and query transactions/alerts, with pagination and filtering.

## Contents

- [Architecture](#architecture)
- [Fraud rules](#fraud-rules)
- [Tech stack](#tech-stack)
- [Quickstart (Docker)](#quickstart-docker)
- [Running locally without Docker](#running-locally-without-docker)
- [Configuration](#configuration)
- [API reference](#api-reference)
- [Database & migrations](#database--migrations)
- [Testing](#testing)
- [Observability](#observability)
- [Production-readiness: what's in, what's deliberately out](#production-readiness-whats-in-whats-deliberately-out)
- [Project structure](#project-structure)

## Architecture

```
server/
├── FraudEngine.Core/     Domain models, EF Core DbContext + migrations, repository, fraud rules
├── FraudEngine.Api/      Controllers, DTOs, validators, middleware, DI wiring, Dockerfile
└── FraudEngine.Tests/    xUnit tests (rules, controllers, middleware, validators, serialization)
```

The rule engine is intentionally pluggable: every rule implements `IFraudRule`

```csharp
public interface IFraudRule
{
    string Name { get; }
    Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx);
}
```

`RulesEngine` takes `IEnumerable<IFraudRule>` from DI and runs all of them against every transaction — adding a new rule means implementing the interface and registering it in `Program.cs`; nothing else changes. Each rule fails independently (a single rule throwing doesn't stop the others, and doesn't fail the request that created the transaction — see [Observability](#observability)).

**Request flow for `POST /api/transactions`:** validate the request (FluentValidation) → map to a `TransactionEvent` → persist it → run it through every registered `IFraudRule` → persist any resulting `FraudAlert`s → return the transaction plus the alerts it triggered.

## Fraud rules

| Rule | Criteria | Default threshold | Severity |
|---|---|---|---|
| `HighAmountRule` | Single transaction amount | ≥ R10,000 | High |
| `RapidTransactionsRule` | Transaction count for an account in a rolling window | ≥ 5 in 1 minute | Medium |
| `VelocityAmountRule` | Cumulative amount for an account in a rolling window | ≥ R50,000 in 24h | High |
| `UnusualCountryRule` | Transaction country vs. the account's home country | any mismatch | High |
| `NightTimeWithdrawalRule` | ATM withdrawal, High risk-tier account, time of day | 00:00–03:59 UTC | Medium |
| `MerchantCategoryRule` | Merchant category code (MCC) | in the high-risk MCC list (crypto, gambling, pawn shops, jewelry) | High |
| `AccountAgeRule` | Account age vs. transaction amount | account < 30 days old **and** amount ≥ R5,000 | High |

All thresholds are configuration-driven via the `RuleOptions` section of `appsettings.json` (bound through `IOptions<RuleOptions>` — no code change needed to retune them):

```json
"RuleOptions": {
  "HighAmountThreshold": 10000,
  "RapidTransactionCount": 5,
  "RapidTransactionWindow": "00:01:00",
  "VelocityAmountThreshold": 50000,
  "VelocityAmountWindow": "1.00:00:00",
  "AccountAgeThresholdDays": 30,
  "AccountAgeLargeAmountThreshold": 5000,
  "HighRiskMerchantCategoryCodes": [ "6051", "7995", "5933", "5944" ]
}
```

## Tech stack

.NET 8 / ASP.NET Core Web API · EF Core 8 + Npgsql · PostgreSQL 15 · FluentValidation · Serilog (structured JSON logging) · Swashbuckle/Swagger · xUnit · Docker & Docker Compose.

## Quickstart (Docker)

Requires Docker & Docker Compose.

```bash
cd server
docker compose up --build
```

- API: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`

On first run (in the `Development` environment, which `docker-compose.yml` sets for the `api` service), the database is seeded automatically with 27 accounts and 200+ transactions — including one guaranteed, real scenario per fraud rule — so `GET /api/transactions/alerts` returns results immediately with no manual setup. Seeding is a no-op once the database has data, and never runs outside `Development`.

To get a fully fresh environment (new seed data, clean schema):

```bash
docker compose down -v && docker compose up --build
```

## Running locally without Docker

Requires the .NET 8 SDK and a running PostgreSQL instance.

```bash
# start just Postgres via compose, or point at your own instance
cd server
docker compose up postgres -d

dotnet build
dotnet run --project FraudEngine.Api
```

`appsettings.Development.json` defaults to `Host=localhost;Port=5432;Database=fraud_engine;Username=postgres;Password=postgrespw` — override via the `ConnectionStrings__DefaultConnection` environment variable if your local Postgres differs.

## Configuration

| Setting | How | Notes |
|---|---|---|
| DB connection string | `ConnectionStrings:DefaultConnection` in `appsettings.json`, or `ConnectionStrings__DefaultConnection` env var | Empty by design in `appsettings.json` (Production) — the app fails fast at startup with a clear error rather than silently using a hardcoded value. Never commit real credentials. |
| Fraud rule thresholds | `RuleOptions` section, see [Fraud rules](#fraud-rules) | Per-environment override via `appsettings.{Environment}.json` or env vars. |
| Environment | `ASPNETCORE_ENVIRONMENT` | Controls Swagger UI, dev seeding, and exception detail exposure (see [Observability](#observability)). |
| Logging | Serilog, configured in `Program.cs` | JSON to console + `logs/fraud-engine-*.log` (daily rolling file). |

## API reference

All request/response enums (`Category`, `Channel`, `Status`, `Severity`, ...) serialize as readable strings (`"High"`, `"Payment"`, `"Open"`) in both JSON and the Swagger schema — not raw integers. Numeric values are still accepted on input for backward compatibility.

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/transactions` | Submit a transaction; runs it through all fraud rules and returns the transaction plus any alerts raised. |
| `GET` | `/api/transactions/{id}` | Fetch a single transaction by id. `404` if not found. |
| `GET` | `/api/transactions?accountId=&category=&from=&to=&page=&pageSize=` | Paginated, filterable transaction listing. All filters optional; `page`/`pageSize` are clamped to sane bounds (`pageSize` max 100). |
| `GET` | `/api/transactions/alerts?status=` | List fraud alerts, optionally filtered by status (`Open`/`UnderReview`/`Resolved`/`FalsePositive`). |
| `PATCH` | `/api/alerts/{id}/status` | Update an alert's review status (`{ "status": "Resolved", "reviewedBy": "analyst-1" }`). `404` if the alert doesn't exist. |
| `POST` | `/api/accounts` | Create an account (used by `UnusualCountryRule`, `NightTimeWithdrawalRule`, `AccountAgeRule`). `409` if the `accountId` already exists. |
| `GET` | `/api/accounts/{id}` | Fetch a single account by id. `404` if not found. |
| `GET` | `/health` | Liveness/readiness — overall status plus Postgres connectivity. |

Example:

```bash
curl -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
        "accountId": "acct-high-001",
        "amount": 15000,
        "category": "Withdrawal",
        "channel": "Branch",
        "countryCode": "ZA"
      }'
```

Every unhandled error returns a standard RFC 7807 `application/problem+json` body (`status`, `title`, `detail` — Development only, `traceId`), and every response carries an `X-Correlation-Id` header (generated if the caller didn't send one) for tracing a request across logs.

## Database & migrations

Migrations live in `FraudEngine.Core/Data/Migrations` and are applied automatically at startup (`db.Database.MigrateAsync()` in `Program.cs`) — no manual step needed for `docker compose up`.

```bash
cd server

# add a migration after changing an entity / FraudDbContext
dotnet tool install --global dotnet-ef   # first time only
dotnet ef migrations add <MigrationName> --project FraudEngine.Core --startup-project FraudEngine.Api --output-dir Data/Migrations

# apply migrations manually instead of relying on startup
dotnet ef database update --project FraudEngine.Core --startup-project FraudEngine.Api

# generate a SQL script for a DBA / manual deployment
dotnet ef migrations script --project FraudEngine.Core --startup-project FraudEngine.Api -o migrate.sql
```

## Testing

```bash
cd server
dotnet test
```

100 xUnit tests, no database required (an in-memory `IRepository` test double stands in for EF Core):

- **Rules** — all 7 fraud rules: triggers, non-triggers, and boundary conditions.
- **Controllers** — `TransactionsController`, `AlertsController`, `AccountsController`: status codes, pagination clamping, not-found/conflict handling.
- **Middleware** — `GlobalExceptionMiddleware` (problem+json shape, already-started-response guard), `RulesEngine` (per-rule failure isolation).
- **Validators** — `TransactionRequestValidator`, `UpdateAlertStatusRequestValidator`, `AccountRequestValidator` (including an end-to-end test that the validator is actually wired into the request pipeline).
- **Serialization** — enum-as-string JSON round-tripping, including backward compatibility with numeric input.

## Observability

- **Structured logging** — Serilog, JSON to console and `logs/fraud-engine-*.log` (daily rolling), enriched with machine name and log context.
- **Correlation IDs** — `CorrelationIdMiddleware` reads/generates `X-Correlation-Id`, pushes it into every log entry for the request, and echoes it back in the response header.
- **Health checks** — `GET /health` reports overall status plus a live Postgres connectivity check (`{"status": "Healthy", "checks": [{"name": "postgres", ...}], "totalDuration": ...}`), without leaking internal error detail (hostnames, credentials) to unauthenticated callers.
- **Resilience** — EF Core retries transient Postgres connection failures automatically (`EnableRetryOnFailure`); a single fraud rule throwing doesn't abort evaluation of the others.

## Production-readiness: what's in, what's deliberately out

**In:**
- Input validation (FluentValidation + DataAnnotations) with standard `400` responses
- Centralized, RFC 7807-compliant error handling for unhandled exceptions
- Structured logging + correlation IDs + health checks (above)
- Nullable reference types enabled project-wide, 0 build warnings
- Automatic schema migrations, connection retry-on-failure
- 100 automated tests covering rules, controllers, middleware, and validators
- Docker Compose with `restart: unless-stopped` and a Postgres healthcheck gating API startup

**Deliberately out of scope** (per the project's own scoping — not oversights, but worth knowing before calling this "done"):
- **Authentication / authorization** — no JWT/OAuth2; every endpoint is open. Required before any real deployment.
- **Full account CRUD** — `AccountsController` covers create (`POST /api/accounts`) and lookup-by-id (`GET /api/accounts/{id}`) only, which is what's needed so `UnusualCountryRule`, `NightTimeWithdrawalRule`, and `AccountAgeRule` have real accounts to evaluate against outside of seeded dev data. Update, delete, and list-all endpoints are deliberately not implemented — not needed to close that gap, and would be scope creep.
- **Rate limiting** — not implemented; intended to be handled at the infrastructure layer (API gateway / reverse proxy) if deployed for real.
- **Multi-tenancy, rule DSL/admin UI, event sourcing or message queues** — the synchronous request/response model and code-based rule registration are sufficient for this scope; these were explicitly not pursued.
- **CI/CD pipeline** — none configured in this repository; `dotnet build`/`dotnet test` are the manual gate today.

## Project structure

```
server/
├── FraudRuleEngine.slnx
├── docker-compose.yml
├── FraudEngine.Core/
│   ├── Data/                  FraudDbContext, DbSeeder, EF migrations
│   ├── Models/                TransactionEvent, FraudAlert, Account, enums
│   ├── Repositories/          IRepository, EfRepository
│   └── Rules/                 IFraudRule, RulesEngine, RuleOptions, the 7 rule implementations
├── FraudEngine.Api/
│   ├── Controllers/           TransactionsController, AlertsController, AccountsController
│   ├── Dtos/                  Request/response DTOs + mapping extensions
│   ├── Validators/             FluentValidation validators
│   ├── Middleware/             GlobalExceptionMiddleware, CorrelationIdMiddleware
│   ├── Dockerfile
│   └── Program.cs
└── FraudEngine.Tests/          xUnit tests + in-memory repository test double
```
