# NextDrop — Sprint 2 Repository & Architecture Audit

**Date:** 2026-08-30  
**Architect:** Lead Software Architect & Senior .NET Engineer  
**Sprint:** Sprint 2 — Customer & Restaurant Foundation  

---

## 1. Existing Architecture & Infrastructure Review

Following the successful execution of Sprint 1, the repository contains a solid, production-grade Modular Monolith baseline:

* **Framework:** .NET 9.0 (`net9.0`)
* **Solution Topology:** `NextDrop.slnx` containing 11 projects across `src/` and `tests/`.
* **Shared Kernel (`NextDrop.SharedKernel`):**
  Contains generic domain primitives: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Result`, `Error`, `IDomainEvent`, `IDateTimeProvider`, `IUnitOfWork`, `ICacheService`, `IMessagePublisher`, `IIdempotencyService`.
* **Identity Module (`Modules/Identity`):**
  Domain aggregates (`User`), `RefreshToken` with family rotation & reuse detection, `EmailVerificationToken`, ASP.NET Core `PasswordHasher<User>`, JWT token generator with lean claims, and MediatR command handlers.
* **Infrastructure (`NextDrop.Infrastructure`):**
  `NextDropDbContext` with EF Core, PostgreSQL 17 configuration, `DomainEventsToOutboxInterceptor`, `OutboxProcessorJob`, Redis cache service, RabbitMQ publisher, and System clock.
* **API (`NextDrop.Api`):**
  ASP.NET Core Web API with Controllers, `CorrelationIdMiddleware`, `GlobalExceptionHandler` (RFC 7807 ProblemDetails), `IdempotencyFilter`, Rate Limiter (`auth-policy`), JWT Bearer Authentication, Policy Authorization, Swagger OpenAPI, and `/health` endpoints.
* **Testing & Quality Baseline:**
  100% passing test suite (20/20 tests passed) across Domain, Application, Infrastructure, NetArchTest Architecture assertions, and WebApplicationFactory Integration tests.

---

## 2. Reusable Infrastructure for Sprint 2

Sprint 2 will directly consume existing Sprint 1 infrastructure without parallel abstractions or duplication:

1. **SharedKernel Abstractions:** `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Result`, `Error`, `IDateTimeProvider`, `IUnitOfWork`, `ICacheService`, `IMessagePublisher`.
2. **Domain Events & Outbox:** All new domain events raised by `Customer` and `Restaurant` aggregates will be automatically intercepted by `DomainEventsToOutboxInterceptor` and persisted to the PostgreSQL outbox table in the same transaction.
3. **Authentication & Authorization:** Sprint 1 JWT Bearer token authentication provides identity claims (`sub` = UserId, `email`, `roles`). Sprint 2 will build resource-based authorization handlers on top of `UserId`.
4. **Idempotency & Middleware:** Controllers for Customer and Restaurant management will leverage existing `[IdempotencyFilter]` and `CorrelationIdMiddleware`.
5. **Database Persistence:** `NextDropDbContext` will be configured with EF Core entity configurations for Customer and Restaurant domain entities under appropriate schemas (`customers` and `restaurants`).
6. **Testing Harness:** API integration tests will use existing `WebApplicationFactory` fixture with in-memory / test-database setups.

---

## 3. Required Changes for Sprint 2

1. **New Projects:**
   - `src/Modules/Customers/NextDrop.Modules.Customers.Domain`
   - `src/Modules/Customers/NextDrop.Modules.Customers.Application`
   - `src/Modules/Customers/NextDrop.Modules.Customers.Infrastructure`
   - `src/Modules/Restaurants/NextDrop.Modules.Restaurants.Domain`
   - `src/Modules/Restaurants/NextDrop.Modules.Restaurants.Application`
   - `src/Modules/Restaurants/NextDrop.Modules.Restaurants.Infrastructure`
2. **Module Registration:**
   - Dependency injection extensions for Customers and Restaurants modules registered in `NextDrop.Api` `Program.cs`.
3. **EF Core Database Schema Additions:**
   - Customer tables (`customers.customers`, `customers.customer_addresses`, `customers.customer_preferences`).
   - Restaurant tables (`restaurants.restaurants`, `restaurants.restaurant_branches`, `restaurants.operating_hours`, `restaurants.delivery_zones`, `restaurants.staff_memberships`).
4. **Resource Authorization:**
   - `RestaurantAuthorizationHandler` dynamically checking if `CurrentUser` (UserId) is the Owner or Staff of `RestaurantId`.
5. **EF Core Migration:**
   - Migration `AddCustomerAndRestaurantDomains` in `NextDrop.Infrastructure`.

---

## 4. Architectural Risks & Mitigation Strategies

| Risk | Impact | Mitigation Strategy |
| :--- | :--- | :--- |
| **Tight Coupling to Identity Module** | Breaking Modular Monolith boundaries by directly querying Identity entities in Customer/Restaurant modules | Customer and Restaurant aggregates reference Identity strictly by `UserId` struct/GUID. No direct entity cross-references. Enforced by NetArchTest architecture rules. |
| **Race Conditions on Default Address** | Multiple concurrent requests setting different default addresses | Handled via transactional encapsulation in application handler and database partial unique index / constraints. |
| **Overnight Operating Hours Bug** | Incorrectly rejecting valid orders placed past midnight (e.g., 23:00 to 02:00) | Represent open/close times as `TimeOnly` with explicit domain helper `IsOpenAt(TimeOnly localTime)` handling `CloseTime < OpenTime`. |
| **N+1 Query Overhead on Public Listing** | Loading entire entity graphs (Branches, Staff, Hours, Zones) on restaurant search | Read queries use DTO projection (`Select(r => new RestaurantSummaryDto...)`) avoiding navigation tracking and N+1 queries. |
| **Broken Object-Level Authorization (IDOR)** | Customer A modifying Customer B's address or Owner A updating Owner B's restaurant | Mandatory server-side authorization check deriving `UserId` from JWT claims and matching against aggregate owner/customer IDs before mutation. |

---

## 5. Migration Strategy

1. Maintain existing `20260830091847_InitialCreate` migration.
2. Add new EF Core migration `20260830_AddCustomerAndRestaurantDomains` targeting PostgreSQL schema separation (`customers` and `restaurants`).
3. Keep `ApplyMigrationsOnStartup = false` for production deployments.
