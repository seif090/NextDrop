# ADR-003: Cart & Order Domain Boundaries, Price Snapshotting & State Machine

**Status:** Accepted  
**Date:** 2026-08-30  
**Deciders:** Lead Software Architect & Engineering Team  

---

## Context & Problem Statement

Marketplace ordering systems often suffer from:
1. Client-side price tampering or stale cart totals being trusted at checkout.
2. In-place catalog mutations retroactively modifying historical order receipts.
3. Uncontrolled order cancellation or invalid status transitions.
4. Idempotency failures leading to duplicate order creation during network retries.

---

## Decision Drivers

- **Server-Side Price Resolution:** Always re-evaluate prices against active catalog records before generating order totals.
- **Historical Snapshotting:** `OrderItem` and `OrderDeliveryAddress` are stored as immutable snapshots detached from mutable foreign entity records.
- **Strict Encapsulated State Machine:** Status transitions are managed solely by domain methods inside `Order` aggregate.
- **Idempotency Integration:** Enforce `Idempotency-Key` headers on checkout operations using `IIdempotencyService`.

---

## Decision Outcome

1. **Cart Aggregate:** Scope items to a single customer & restaurant branch.
2. **Order Aggregate:** Encapsulate pricing calculations, min-order checks, delivery fee resolution, and state machine transitions.
3. **Optimistic Concurrency:** Protect against concurrent stale checkouts using PostgreSQL `xmin` concurrency tokens (`RowVersion`).
