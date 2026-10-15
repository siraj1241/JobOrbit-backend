# 🛰️ JobOrbit Backend

A production-ready **ASP.NET Core 9 Web API** for a job board platform — authentication, job listings, and applications. Built with **Clean Architecture**, **JWT auth**, **PostgreSQL**, **FluentValidation**, and **Serilog**.

---

## 📐 Architecture

The solution follows a **layered (Clean) Architecture** with strict dependency direction:

```
┌────────────────────────────────────────────────────────────┐
│  JoborbitApi  (Presentation / API layer)                   │
│  • Controllers, Middlewares, Program.cs                    │
│  • Depends on: Application + Infrastructure               │
└──────────────────┬─────────────────────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────────────────────┐
│  JobsApi.Application  (Use-case layer)                    │
│  • DTOs, Validators, Abstractions (interfaces)            │
│  • Depends on: Domain (no EF, no DB, no HTTP)             │
└──────────────────┬─────────────────────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────────────────────┐
│  JobsApi.Infrastructure  (Implementation layer)           │
│  • Repositories, Auth services, DbContext, Migrations      │
│  • Depends on: Application + Domain                       │
└──────────────────┬─────────────────────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────────────────────┐
│  JobsApi.Domain  (Enterprise business rules)              │
│  • Entities, Exceptions                                  │
│  • Depends on: NOTHING (pure C#)                         │
└────────────────────────────────────────────────────────────┘
```

**Dependency rule:** dependencies always point **inward**. The Domain has zero external references. Application depends only on Domain. Infrastructure implements Application's abstractions. The API project wires everything together at startup.

---

## 🗂️ Project Structure

```
JobOrbit-backend/
│
├── JoborbitApi/                          # 🌐 Presentation Layer (ASP.NET Core Web API)
│   ├── Controllers/
│   │   ├── AccountController.cs          #   /api/account → login, register, me
│   │   ├── JobsController.cs             #   /api/jobs → external, search, sync, by-id
│   │   └── WeatherForecastController.cs  #   sample controller (placeholder)
│   │
│   ├── Middlewares/
│   │   └── ExceptionMiddleware.cs       #   Global exception → ProblemDetails
│   │
│   ├── Properties/
│   │   └── launchSettings.json          #   http://localhost:5087
│   │
│   ├── Program.cs                        #   App composition root (DI, JWT, Swagger, Serilog)
│   ├── appsettings.json                  #   Connection string + JWT + ExternalJobs config
│   ├── appsettings.Development.json
│   └── JoborbitApi.http                  #   Sample requests for VS / Rider REST client
│
├── JobsApi.Application/                  # 📋 Use-Case Layer
│   ├── Abstractions/
│   │   ├── IAuthService.cs               #   LoginAsync, RegisterAsync
│   │   ├── IUserRepository.cs            #   GetByEmail, GetById, Add, Update, Exists
│   │   ├── ITokenService.cs              #   GenerateAccessToken(User)
│   │   ├── IJobRepository.cs             #   GetById, GetByExternalId, Search, Upsert
│   │   ├── IJobService.cs                #   GetExternal, Search, GetById, SyncFromExternal
│   │   └── IExternalJobProvider.cs       #   FetchAsync (Abbeitnow contract)
│   │
│   ├── Dtos/
│   │   ├── AuthDtos.cs                    #   LoginDto, RegisterDto, AuthResultDto
│   │   └── JobDtos.cs                    #   JobDto, JobsQueryDto, PagedResultDto, SyncResultDto
│   │
│   └── Validators/
│       ├── LoginValidator.cs             #   FluentValidation rules for LoginDto
│       ├── RegisterValidator.cs          #   FluentValidation rules for RegisterDto
│       └── JobQueryValidators.cs          #   FluentValidation rules for JobsQueryDto
│
├── JobsApi.Infrastructure/               # 🏗️ Implementation Layer
│   ├── Auth/
│   │   ├── AuthService.cs                #   Implements IAuthService (login + register)
│   │   └── JwtTokenService.cs            #   Implements ITokenService (JWT generation)
│   │
│   ├── ExternalJobs/
│   │   └── ArbeitnowJobProvider.cs       #   Implements IExternalJobProvider via HttpClient + retry
│   │
│   ├── Jobs/
│   │   └── JobService.cs                 #   Implements IJobService (orchestrates fetch + sync)
│   │
│   ├── Repositories/
│   │   ├── UserRepository.cs             #   Implements IUserRepository (EF Core)
│   │   └── JobRepository.cs              #   Implements IJobRepository (EF Core + ILIKE search)
│   │
│   ├── Persistence/
│   │   ├── JobsDbContext.cs              #   EF Core DbContext (User + Job entities)
│   │   └── DbSeeder.cs                  #   Seeds a test user on startup
│   │
│   ├── Migrations/                       #   EF Core migration history
│   │   ├── 20261008172149_InitialCreate.cs
│   │   ├── 20261008172255_AddSomething.cs
│   │   ├── 20261008172519_SeedRoles.cs
│   │   ├── 20261008172808_AddUserChanges.cs
│   │   ├── 20261015120000_AddJobsEntity.cs
│   │   └── JobsDbContextModelSnapshot.cs
│   │
│   ├── Options/
│   │   ├── JwtOptions.cs                 #   Strongly-typed Jwt config (validated on start)
│   │   └── ExternalJobsOptions.cs        #   Arbeitnow API base URL, retry, timeout config
│   │
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs#   AddJobsApiInfrastructure() DI helper
│
├── JobsApi.Domain/                       # 🧱 Domain Layer (pure C#, no dependencies)
│   ├── Entities/
│   │   ├── User.cs                       #   User entity (Id, Email, PasswordHash, Role, ...)
│   │   └── Job.cs                        #   Job entity (Id, ExternalId, Source, Title, ...)
│   │
│   └── Exceptions/
│       └── DomainException.cs            #   Base + UnauthorizedException, NotFoundException, ConflictException
│
├── JoborbitApi.sln                       # 🔗 Visual Studio solution file
└── README.md                             # 📖 This file
```

---

## 🧱 Layer Responsibilities

### 1. `JobsApi.Domain`
The heart of the system. Pure C# with **no external dependencies**. Holds the entities (currently `User`) and the domain exception hierarchy. This is the only project that other layers cannot reference back into.

### 2. `JobsApi.Application`
Defines the **contracts** of the system: DTOs, FluentValidation rules, and the abstraction interfaces (`IAuthService`, `IUserRepository`, `ITokenService`). It declares *what* the system does without deciding *how*. This is where future use-case handlers / CQRS commands would live.

### 3. `JobsApi.Infrastructure`
Implements the Application abstractions using concrete technologies:
- **EF Core** with **PostgreSQL** (`JobsDbContext`, `UserRepository`)
- **ASP.NET Core Identity** `PasswordHasher<User>` for bcrypt-style password hashing
- **JWT** token generation (`JwtTokenService`)
- **AuthService** orchestrates login & registration flows
- EF Core **migrations** for schema evolution
- `DbSeeder` seeds a test user on startup for manual testing

### 4. `JoborbitApi`
The composition root. Wires DI, configures JWT bearer auth, Swagger UI, Serilog, and the global exception middleware. Hosts the HTTP controllers.

---

## 🛠️ Tech Stack

| Concern               | Library / Tool                                            |
|-----------------------|-----------------------------------------------------------|
| Runtime               | .NET 9                                                    |
| Web framework         | ASP.NET Core 9 Web API                                    |
| ORM                   | Entity Framework Core 9 (Code-First + Migrations)        |
| Database              | PostgreSQL (via `Npgsql.EntityFrameworkCore.PostgreSQL`) |
| Auth                  | JWT Bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Password hashing      | ASP.NET Core Identity `PasswordHasher<User>`             |
| Validation            | FluentValidation 12                                       |
| Logging               | Serilog + Console sink                                    |
| API docs              | Swashbuckle / Swagger UI                                  |
| Error format          | RFC 7807 `ProblemDetails` (via custom middleware)        |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL instance (local or cloud — e.g. Supabase, Neon, or Docker)

### 1. Clone
```bash
git clone https://github.com/siraj1241/JobOrbit-backend.git
cd JobOrbit-backend
```

### 2. Configure connection string
Edit `JoborbitApi/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=<your-host>;Port=5432;Database=postgres;Username=<user>;Password=<pwd>;SSL Mode=Require;Trust Server Certificate=true"
},
"Jwt": {
  "Secret": "<at-least-32-char-random-string>",
  "Issuer": "JobOrbit",
  "Audience": "JobOrbit.Clients",
  "AccessMinutes": 15
}
```
> ⚠️ **Never commit real secrets to git.** Use `User Secrets`, environment variables, or Azure Key Vault for production.

### 3. Apply migrations & run
```bash
cd JoborbitApi
dotnet restore
dotnet ef database update --project ../JobsApi.Infrastructure --startup-project .
dotnet run
```
The API starts on **http://localhost:5087** (Development profile).

### 4. Explore via Swagger
Open `http://localhost:5087/swagger` in your browser. You'll see all endpoints + a **JWT Bearer** auth button.

---

## 🌱 Database Seeding

On startup, `DbSeeder.SeedTestUserAsync()` automatically:
- Runs `db.Database.MigrateAsync()` (applies any pending migrations)
- Seeds a test user if one doesn't exist:
  - **Email:** `test@joborbit.com`
  - **Password:** `Password@123`
  - **Role:** `User`
- Logs a warning if Postgres is unreachable (won't crash the app)

---

## 🔐 Authentication Endpoints

All auth endpoints are under `/api/account`.

### Register
```http
POST /api/account/register
Content-Type: application/json

{
  "email": "newuser@joborbit.com",
  "password": "Password@123",
  "confirmPassword": "Password@123",
  "displayName": "New User"
}
```
**201 Created**
```json
{
  "accessToken": "eyJhbGc...",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "newuser@joborbit.com",
  "displayName": "New User",
  "role": "User"
}
```
- **400 Bad Request** — validation failed (email format, password too weak, passwords don't match)
- **409 Conflict** — email already registered

**Password rules** (enforced by `RegisterValidator`):
- Minimum 6 characters (max 64)
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- Must match `confirmPassword`

### Login
```http
POST /api/account/login
Content-Type: application/json

{
  "email": "test@joborbit.com",
  "password": "Password@123"
}
```
**200 OK** — same response shape as register. Returns a fresh JWT.
**401 Unauthorized** — invalid credentials (same message for unknown email vs bad password — prevents user enumeration).

### Get Current User
```http
GET /api/account/me
Authorization: Bearer <jwt-from-login-or-register>
```
```json
{
  "userId": "...",
  "email": "...",
  "role": "User"
}
```

---

## 💼 Jobs API

The Jobs API integrates with the **Arbeitnow public job board** (https://www.arbeitnow.com/api/job-board-api — no auth required) to provide live job listings, with optional local caching for fast search and filtering.

All jobs endpoints live under `/api/jobs`.

### 1. Live fetch from external provider (no persistence)
Returns the latest jobs straight from Arbeitnow — no DB writes.
```http
GET /api/jobs/external?search=python&location=remote&remote=true&page=1&pageSize=20
```
**Query params** (all optional):
- `search` — substring match on title, description, company name
- `location` — substring match on location
- `remote` — `true` / `false`
- `page` (default 1) — page number
- `pageSize` (default 20, max 50)

**200 OK**
```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "externalId": "12345",
      "source": "Arbeitnow",
      "title": "Senior Python Developer",
      "companyName": "Acme Corp",
      "description": "...",
      "location": "Remote",
      "remote": true,
      "tags": ["python", "django", "aws"],
      "jobTypes": ["full_time"],
      "url": "https://www.arbeitnow.com/jobs/12345",
      "publishedAt": "2026-10-15T10:00:00Z",
      "cachedAt": "2026-10-15T11:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

### 2. Search cached jobs (DB)
Returns jobs previously synced into our local Postgres. Supports ILIKE search and tag filtering.
```http
GET /api/jobs?search=developer&remote=true&tag=python&page=1&pageSize=20
```
**Query params** (all optional):
- `search`, `location`, `remote` — same as above
- `tag` — substring match on the comma-separated tags field
- `page` (default 1) — page number
- `pageSize` (default 20, max 100)

### 3. Get a single cached job
```http
GET /api/jobs/{id}
```
**200 OK** — same shape as one element of `items[]`
**404 Not Found** — if the ID doesn't exist in our cache

### 4. Sync external jobs into local DB
Pulls the latest jobs from Arbeitnow and upserts them into the `Jobs` table. Use this to populate the cache periodically (e.g. via a cron job).
```http
POST /api/jobs/sync?maxPages=3
Authorization: Bearer <jwt>
```
- `maxPages` (default 3, clamped to 1–10) — how many Arbeitnow pages to fetch
- **Requires authentication** (any logged-in user; Admin recommended for production)

**200 OK**
```json
{
  "fetched": 150,
  "added": 142,
  "updated": 8,
  "skipped": 0,
  "syncedAt": "2026-10-15T11:30:00Z"
}
```

### How external fetching works
1. `ArbeitnowJobProvider` (typed `HttpClient`) calls `https://www.arbeitnow.com/api/job-board-api?page={n}`
2. Response is deserialized into strongly-typed POCOs
3. Built-in **exponential-backoff retry** (3 attempts, base delay 500 ms) on transient HTTP failures
4. 4xx errors (except 429) fail fast as `DomainException` → 400 to caller
5. Timeouts retry as transient failures

### How sync works
1. Iterate up to `maxPages` from Arbeitnow
2. For each external job: if not in DB → insert; if in DB but changed → update; otherwise skip
3. Returns summary counts in `SyncResultDto`

---

## 🔑 JWT Token

The JWT issued by `JwtTokenService` contains these claims:

| Claim            | Source                    |
|------------------|---------------------------|
| `sub`            | `User.Id` (GUID)          |
| `email`          | `User.Email`              |
| `name`           | `User.DisplayName`        |
| `role`           | `User.Role`               |
| `jti`            | Random GUID (token ID)    |
| `iat`            | Issued-at timestamp       |

Signed with **HMAC-SHA256**, validated by the bearer middleware (issuer, audience, signing key, lifetime — with 30-second clock skew).

---

## ⚠️ Error Handling

Global `ExceptionMiddleware` translates domain exceptions into RFC 7807 `ProblemDetails`:

| Exception                 | HTTP Status | When                                |
|---------------------------|-------------|--------------------------------------|
| `UnauthorizedException`   | 401         | Bad email or password on login       |
| `ConflictException`       | 409         | Email already taken on register      |
| `NotFoundException`       | 404         | Future use (e.g. resource lookups)   |
| `DomainException` (base)  | 400         | Generic domain rule violation       |
| Any other `Exception`      | 500         | Unhandled — logged, message hidden in production |

Example error response:
```json
{
  "status": 409,
  "title": "Conflict",
  "detail": "An account with email 'x@y.com' already exists.",
  "instance": "/api/account/register"
}
```

---

## 📜 Logging

[**Serilog**](https://serilog.net/) is configured in `Program.cs` with:
- Console sink (extendable to file / Seq / Elasticsearch)
- `Information` minimum level
- `Microsoft.AspNetCore` filtered to `Warning`
- Enriched from log context

---

## 📦 Solution Projects

| Project                       | Type             | Target    |
|-------------------------------|------------------|-----------|
| `JoborbitApi`                 | Web (entrypoint) | `net9.0`  |
| `JobsApi.Application`         | Class library    | `net9.0`  |
| `JobsApi.Infrastructure`     | Class library    | `net9.0`  |
| `JobsApi.Domain`              | Class library    | `net9.0`  |

### Restore / build / test
```bash
dotnet restore JoborbitApi.sln
dotnet build JoborbitApi.sln
dotnet test JoborbitApi.sln        # (no test project yet — see Roadmap)
```

---

## 🧭 Entity Model (current)

### `User`
| Field           | Type        | Notes                                   |
|-----------------|-------------|------------------------------------------|
| `Id`            | `Guid`      | PK, default `Guid.NewGuid()`             |
| `Email`         | `string`    | Unique index, max 256, required          |
| `PasswordHash`  | `string`    | Hashed via `PasswordHasher<User>`, 512 max|
| `DisplayName`   | `string`    | Required, max 100                        |
| `Role`          | `string`    | Default `"User"`, max 50                |
| `CreatedAt`     | `DateTime`  | UTC timestamp, default `DateTime.UtcNow`|
| `LastLoginAt`   | `DateTime?` | Updated on every successful login        |

### `Job`
| Field           | Type        | Notes                                                |
|-----------------|-------------|------------------------------------------------------|
| `Id`            | `Guid`      | PK                                                   |
| `ExternalId`    | `string`    | Provider's ID; unique together with `Source`         |
| `Source`        | `string`    | `"Arbeitnow"`, max 50                                |
| `Title`         | `string`    | Required, max 300                                     |
| `CompanyName`   | `string`    | Required, max 200                                     |
| `Description`   | `string`    | `text` (PostgreSQL), required                         |
| `Location`      | `string`    | Required, max 200                                     |
| `Remote`        | `bool`      | Indexed                                              |
| `Tags`          | `string`    | Comma-separated, max 500                              |
| `JobTypes`      | `string`    | Comma-separated, max 200                              |
| `Url`           | `string`    | External listing URL, max 1000                        |
| `PublishedAt`   | `DateTime`  | Original publish date from provider, indexed          |
| `CachedAt`      | `DateTime`  | When we last refreshed the row                        |

---

## 🗺️ Roadmap

- [x] External job API integration (Arbeitnow — live fetch + sync)
- [ ] Refresh tokens (rotating) + token revocation
- [ ] Role-based authorization (`Admin`, `Employer`, `JobSeeker`)
- [ ] Job applications flow (apply, status, history)
- [ ] Email confirmation on registration
- [ ] Password reset flow (token-based)
- [ ] Google OAuth sign-in (`Google.Apis.Auth` is already referenced)
- [ ] Unit + integration tests (xUnit + WebApplicationFactory)
- [ ] CI/CD via GitHub Actions (build + test on PR)
- [ ] Containerize with Docker + `docker-compose.yml`

---

## 📄 License

MIT — feel free to use, modify, and distribute.

---

## 🤝 Contributing

1. Fork the repo
2. Create a feature branch: `git checkout -b feat/your-feature`
3. Commit with conventional commits (`feat:`, `fix:`, `docs:`, ...)
4. Open a Pull Request

---

> Built and maintained by [@siraj1241](https://github.com/siraj1241)
