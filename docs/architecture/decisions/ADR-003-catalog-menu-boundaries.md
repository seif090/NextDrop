# ADR-003: Catalog & Menu Management Domain Boundaries & Concurrency Model

**Status:** Accepted  
**Date:** 2026-08-30  
**Deciders:** Lead Software Architect & Engineering Team  

---

## Context & Problem Statement

Food-delivery marketplace catalog implementations frequently suffer from:
1. Massive single aggregates materializing an entire restaurant menu, leading to memory bloat and concurrency bottlenecks.
2. In-place price mutations destroying historical order pricing context.
3. Stale cache reads when administrative users modify item prices or availability.
4. Broken object-level authorization (IDOR) allowing cross-restaurant menu manipulation.

---

## Decision Drivers

- **Aggregate Size Control:** Separate `Catalog` (categories & lifecycle) from `MenuItem` (pricing, variants, modifiers) to ensure fine-grained concurrency and performance.
- **Optimistic Concurrency:** Protect administrative updates using PostgreSQL `xmin` concurrency tokens (`RowVersion`).
- **Cache Consistency:** Invalidate Redis cache keys (`catalog:public:{restaurantId}`) immediately after database transactions commit.
- **Resource Authorization:** Enforce `UserId + RestaurantId + StaffRole` checks server-side for all catalog modifications.

---

## Decision Outcome

1. **Aggregate Topology:**
   - `Catalog` aggregate owns categories and lifecycle (`Draft`, `Published`, `Archived`).
   - `MenuItem` aggregate owns variants and modifier groups/options.
2. **Pricing & Order Snapshots:** Prices use PostgreSQL `numeric(18,2)`. Future Sprints will capture immutable snapshots of menu item names, variant names, unit prices, and modifier choices at order placement time.
3. **Public Read Model & Caching:** `GET /api/v1/restaurants/{restaurantId}/catalog` returns only `Published` catalogs with active categories and available items, utilizing Redis `IDistributedCache` with post-commit invalidation.
