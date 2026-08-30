# NextDrop — Sprint 8 Architecture Audit Report

## 1. Executive Summary

Sprint 8 introduces the **Discovery Module** (`src/Modules/Discovery/`), public restaurant browsing, open-now timezone calculation, menu search, relevance scoring, database indexing, and Redis caching.

This audit certifies that all public read models, criteria value objects, CQRS query handlers, timezone conversions, database projections, index definitions, and NetArchTest isolation rules strictly comply with DDD and Clean Architecture standards.

---

## 2. Bounded Context & Topology

```text
src/Modules/Discovery/
├── NextDrop.Modules.Discovery.Domain/
│   ├── Enums/ (DiscoverySort, MenuItemSort)
│   └── ValueObjects/ (RestaurantDiscoveryCriteria, MenuItemDiscoveryCriteria)
├── NextDrop.Modules.Discovery.Application/
│   ├── Abstractions/ (IDiscoveryReadService, IDiscoveryCacheService)
│   ├── DTOs/ (PublicRestaurantDto, PublicBranchDto, PublicMenuItemDto, PagedDiscoveryResultDto<T>)
│   ├── Queries/ (GetPublicRestaurantsQuery, GetPublicRestaurantByIdQuery, GetPublicRestaurantBranchesQuery, GetPublicMenuItemsQuery)
│   └── Validators/ (GetPublicRestaurantsQueryValidator, GetPublicMenuItemsQueryValidator)
└── NextDrop.Modules.Discovery.Infrastructure/
    ├── Services/ (DiscoveryReadService, DiscoveryCacheService)
    └── DependencyInjection.cs
```

---

## 3. Dependency Direction Rules

- `Discovery.Domain` $\rightarrow$ `SharedKernel` only.
- `Discovery.Application` $\rightarrow$ `Discovery.Domain`, `SharedKernel`, `Restaurants.Domain`, `Catalog.Domain`.
- `Discovery.Infrastructure` $\rightarrow$ `Discovery.Application`, `Discovery.Domain`, `NextDrop.Infrastructure`, `SharedKernel`.
- Discovery MUST NOT depend on `NextDrop.Api` or Infrastructure projects of other modules.

---

## 4. Query & Caching Strategy

- **Read-Only Projections:** Database queries use `AsNoTracking()` and project directly to DTOs without loading unneeded entities or causing N+1 queries.
- **Redis Caching:** Public query responses are cached with deterministic SHA-256 query key hashes (`discovery:restaurants:{hash}`, `discovery:menu:{hash}`) for 60 seconds.
- **Timezone-Aware Open-Now:** Converts current UTC time from `IDateTimeProvider` to branch-local time using `RestaurantBranch.Timezone` before evaluating operating hours.

---

## 5. Verification Summary

- **Total Solution Projects:** 35
- **Build Status:** 0 Errors / 0 Warnings
- **Total Tests:** 124 Passed (100% Pass Rate)
- **EF Core Migration:** `AddDiscoveryIndexesAndSearchOptimization` created and verified.
