# ADR-003: Discovery Query, Relevance & Caching Strategy

## Status
Approved

## Context
NextDrop requires high-performance, public discovery endpoints for browsing active restaurants, searching catalog menu items, and evaluating open-now branch operational status without introducing external search engines (such as Elasticsearch) or violating module boundaries.

## Decision
1. **Lightweight Discovery Read Module:**
   - Implement `NextDrop.Modules.Discovery` for read-only public discovery.
   - Do NOT duplicate Restaurant or Catalog database tables.
   - Execute direct EF Core queries using `AsNoTracking()` and DTO projections.

2. **Deterministic Relevance Scoring:**
   - Restaurant search relevance: Exact Name Match (Score: 100) > Name StartsWith (Score: 50) > Name Contains (Score: 30) > Description Contains (Score: 10).
   - Menu item search relevance: Exact Name Match (100) > Name StartsWith (50) > Name Contains (30) > Description Contains (10).

3. **Timezone-Aware Open-Now Evaluation:**
   - Convert current UTC timestamp (`IDateTimeProvider.UtcNow`) to branch local time using `RestaurantBranch.Timezone` stored IANA string before checking `RestaurantOperatingHours`.

4. **Redis Discovery Caching:**
   - Cache query results in Redis using deterministic SHA-256 parameter hashing.
   - Set 60-second TTL for public discovery endpoints.

## Consequences
- Delivers fast, zero-N+1 public queries.
- Prevents security leaks of internal management attributes (`OwnerUserId`, staff memberships).
