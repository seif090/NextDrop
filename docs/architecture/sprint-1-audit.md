# Sprint 1 Repository Audit — NextDrop

**Date:** 2026-08-30  
**Architect:** Lead Software Architect & Senior .NET Engineer  
**Status:** Complete  

---

## 1. Current State

* **Repository Directory:** `c:\Users\seaif\Desktop\NextDrop`
* **Project Type:** Greenfield / New Project (Repository is completely empty).
* **Environment:**
  * Operating System: Windows 10.0.19045 (win-x64)
  * .NET SDK: 10.0.202 (Supporting .NET 9.0 target framework)
  * Containers: Docker & Docker Compose available

---

## 2. Detected Issues & Gaps

Because this is a clean, empty repository, there are no legacy code defects, architectural debt, or broken implementations to refactor. However, starting greenfield carries specific risks if architectural boundaries are not explicitly defined from Day 1:

1. **Lack of Foundation:** No solution structure, project setup, or domain abstractions.
2. **Security & Configuration Risk:** Risk of hardcoding JWT keys, database connection strings, or relying on insecure fallback defaults if strongly-typed configuration validation is not enforced.
3. **Architectural Drift Risk:** Without explicit architecture tests (e.g. NetArchTest / ArchUnit equivalent), modular boundaries between Domain, Application, Infrastructure, and API could easily be violated as features expand.

---

## 3. Proposed Changes

We will establish a **Modular Monolith** architecture with clean, layered project separation:

### Project Structure (`src/` and `tests/`)

* **`src/NextDrop.Domain`**: Core domain primitives (`Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `Result`, `Error`, strongly typed IDs), shared kernel specifications, and domain exceptions. Zero dependencies on EF Core, ASP.NET Core, or external infrastructure.
* **`src/NextDrop.Application`**: Use cases, CQRS patterns (MediatR / FastEndpoints / services), FluentValidation validators, abstractions for persistence, caching, messaging, background processing, and domain event handlers.
* **`src/NextDrop.Infrastructure`**: EF Core `ApplicationDbContext` (PostgreSQL), Dapper read queries, Redis cache implementation, RabbitMQ message publisher/bus foundation, Hangfire job configuration, Serilog, OpenTelemetry, and Password Hashing / Token services.
* **`src/NextDrop.Api`**: ASP.NET Core Web API host, endpoints (`/api/v1/...`), JWT Bearer authentication, policy-based authorization handlers, centralized ProblemDetails middleware, correlation ID middleware, rate limiting policies, and Swagger/OpenAPI setup.
* **`src/Modules/Identity`**: Specific domain/application abstractions and implementations for Identity & Access (Users, Roles, Registration, Email Verification, Login, Token Refresh/Rotation/Revocation, Password Reset).

### Test Suite (`tests/`)

* **`tests/NextDrop.Domain.Tests`**: Unit tests for domain primitives, invariants, and result patterns.
* **`tests/NextDrop.Application.Tests`**: Unit tests for use cases, validators, and handlers.
* **`tests/NextDrop.Infrastructure.Tests`**: Unit/integration tests for infrastructure services (token generation, password hashing).
* **`tests/NextDrop.Api.Tests`**: WebApplicationFactory integration tests with PostgreSQL container for auth flows, rate limiting, correlation IDs, health checks, and error handling.
* **`tests/NextDrop.Architecture.Tests`**: Architectural rule assertions verifying dependency direction (Domain -> Application -> Infrastructure/Api).

### Infrastructure Support

* **`docker-compose.yml`**: PostgreSQL 17, Redis 7, RabbitMQ 4 (Management enabled).
* **`docs/`**: ADRs, Security Audits, API Conventions, and Sprint Reports.

---

## 4. Files Affected

* `NextDrop.sln`
* `src/NextDrop.Domain/NextDrop.Domain.csproj`
* `src/NextDrop.Application/NextDrop.Application.csproj`
* `src/NextDrop.Infrastructure/NextDrop.Infrastructure.csproj`
* `src/NextDrop.Api/NextDrop.Api.csproj`
* `tests/NextDrop.Domain.Tests/NextDrop.Domain.Tests.csproj`
* `tests/NextDrop.Application.Tests/NextDrop.Application.Tests.csproj`
* `tests/NextDrop.Infrastructure.Tests/NextDrop.Infrastructure.Tests.csproj`
* `tests/NextDrop.Api.Tests/NextDrop.Api.Tests.csproj`
* `tests/NextDrop.Architecture.Tests/NextDrop.Architecture.Tests.csproj`
* `docker-compose.yml`
* `docs/architecture/architecture-overview.md`
* `docs/architecture/decisions/ADR-001-modular-monolith.md`
* `docs/security/sprint-1-security-audit.md`
* `docs/api/conventions.md`

---

## 5. Risks & Mitigations

| Risk | Mitigation |
| :--- | :--- |
| Secret Leakage in Dev Setup | Use options pattern with validation (`ValidateDataAnnotations` & `ValidateOnStart`), fallback rejection for missing secrets. |
| Non-deterministic Integration Tests | Use WebApplicationFactory with Testcontainers (PostgreSQL, Redis) or isolated test DBs. |
| Insecure Refresh Token Handling | Implement SHA-256 hashed refresh tokens in database with rotation, expiration, and reuse detection. |
| Tight Coupling across Modules | Define clear internal interfaces, domain events, and architecture tests enforcing strict layering. |

---

## 6. Migration Strategy

As this is a greenfield setup:
1. Initialize .NET 9 solution and project structure.
2. Configure Core Domain Primitives.
3. Configure Infrastructure (EF Core, Serilog, OpenTelemetry, Redis, RabbitMQ, Hangfire).
4. Implement Identity Domain & Application Services.
5. Configure API layer (Auth, Authorization, Middleware, Health, Rate Limiting, Idempotency).
6. Build comprehensive test suite (Unit, Integration, Architecture).
7. Validate Docker Compose and test runs.
