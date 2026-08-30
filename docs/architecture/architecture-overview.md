# NextDrop Architecture Overview — Sprint 1

**Platform:** NextDrop — On-Demand Delivery Marketplace  
**Architecture Style:** Modular Monolith  
**Framework:** .NET 9.0 (ASP.NET Core Web API)  

---

## 1. Architectural Strategy & Design Principles

NextDrop is built as a **Modular Monolith** to deliver production-grade quality, clean domain encapsulation, and high developer velocity without premature distributed systems complexity. Each domain area is isolated as a distinct module with strict physical and logical boundaries.

### Core Architectural Rules

1. **SharedKernel Primitives Only:**
   `NextDrop.SharedKernel` provides generic domain abstractions (`Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Result`, `Error`, `IDomainEvent`, `IDateTimeProvider`, `IUnitOfWork`, `ICacheService`, `IMessagePublisher`, `IIdempotencyService`). It contains **zero** infrastructure dependencies.

2. **Module Boundary Isolation:**
   Each module (such as `Modules/Identity`) encapsulates its own Domain, Application, and Infrastructure projects/namespaces. Modules interact strictly via public domain events or application interfaces, preventing tight cross-module coupling.

3. **Lean Identity JWT Tokens:**
   Access tokens contain strictly minimal identity claims (`sub`, `email`, `jti`, `roles`). Fine-grained authorization policies (`CanManageRestaurant`, `CanRefundOrder`, etc.) are evaluated dynamically in ASP.NET Core Policy Handlers.

4. **Token-Family Refresh Token Rotation & Reuse Detection:**
   Refresh tokens are stored as SHA-256 hashes inside a `TokenFamilyId` group. Reusing an old/revoked refresh token immediately revokes the **entire token family**, preventing replay attacks.

5. **True Transactional Outbox:**
   Domain events raised during aggregate operations are intercepted by `DomainEventsToOutboxInterceptor` and saved into PostgreSQL `messaging.OutboxMessages` within the **exact same database transaction**. Background job `OutboxProcessorJob` asynchronously publishes outbox messages to RabbitMQ with retry limits and backoff.

6. **Idempotency Guarantee:**
   State-changing requests accepting `Idempotency-Key` headers compute payload SHA-256 hashes. Same key + same payload replays the cached result; same key + different payload returns HTTP `409 Conflict`.

---

## 2. Solution Layout

```text
c:\Users\seaif\Desktop\NextDrop\
├── src\
│   ├── NextDrop.SharedKernel\                     # Common domain & cross-cutting interfaces
│   ├── Modules\Identity\
│   │   ├── NextDrop.Modules.Identity.Domain\      # User aggregate, RefreshToken, EmailToken
│   │   ├── NextDrop.Modules.Identity.Application\ # Commands (Register, Login, Verify, Refresh, Revoke)
│   │   └── NextDrop.Modules.Identity.Infrastructure\# PasswordHasher, JwtTokenGenerator, DevEmailService
│   ├── NextDrop.Infrastructure\                   # EF Core DbContext, PostgreSQL, Redis, RabbitMQ, Outbox
│   └── NextDrop.Api\                              # ASP.NET Core Web API Host, Middleware, Endpoints
└── tests\
    ├── NextDrop.Domain.Tests\                     # Domain primitives & User aggregate tests
    ├── NextDrop.Application.Tests\                # Command handlers & validation unit tests
    ├── NextDrop.Infrastructure.Tests\             # Security & Token hashing unit tests
    ├── NextDrop.Api.Tests\                        # WebApplicationFactory integration test suite
    └── NextDrop.Architecture.Tests\               # NetArchTest architectural assertions
```

---

## 3. Technology Stack Reference

* **Framework:** .NET 9.0 (ASP.NET Core Web API)
* **ORM & Database:** Entity Framework Core 9.0 + PostgreSQL 17
* **Caching:** Redis 7 via StackExchange.Redis
* **Messaging & Outbox:** RabbitMQ 4 + Transactional Outbox Pattern
* **Logging & Observability:** Serilog (Structured Console Logging) + OpenTelemetry
* **Authentication:** JWT Bearer + ASP.NET Core `PasswordHasher<User>`
* **Testing:** xUnit, FluentAssertions, Moq, WebApplicationFactory, NetArchTest.Rules
* **Containers:** Docker & Docker Compose
