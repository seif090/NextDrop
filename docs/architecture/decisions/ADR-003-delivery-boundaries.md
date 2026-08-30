# ADR-003: Rider & Delivery Module Boundaries, State Separation & Concurrency

**Status:** Accepted  
**Date:** 2026-08-30  
**Deciders:** Lead Software Architect & Engineering Team  

---

## Context & Problem Statement

Delivery fulfillment networks require:
1. Clear separation between Order status (customer/restaurant facing) and Delivery status (logistics facing).
2. Concurrency protection to prevent two riders accepting the same delivery simultaneously.
3. Ephemeral high-frequency location caching (Redis) without corrupting PostgreSQL relational truth.
4. Provider-independent distance calculation to allow future mapping integration without rewriting business logic.

---

## Decision Drivers

- **Order vs Delivery Decoupling:** Keep `Order` and `Delivery` as separate aggregates with distinct state machines.
- **Optimistic Concurrency:** Protect against concurrent assignment races using PostgreSQL `xmin` concurrency tokens (`RowVersion`).
- **Provider-Independent Abstraction:** Abstract distance calculation behind `IDistanceCalculator` with a default `HaversineDistanceCalculator`.
- **Ephemeral Location Caching:** Write rider GPS updates to Redis (`rider:{id}:location`) and persist valid locations in `delivery.riders`.

---

## Decision Outcome

1. **Rider Aggregate:** Scope rider profile, vehicle, status, and availability.
2. **Delivery Aggregate:** Scope delivery lifecycle, assignment, pickup, and completion timestamps.
3. **Idempotency Integration:** Enforce `Idempotency-Key` headers on state-changing operations (`accept`, `pickup`, `complete`, `fail`).
