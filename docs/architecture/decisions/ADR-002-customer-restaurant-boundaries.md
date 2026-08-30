# ADR-002: Customer and Restaurant Module Domain Boundaries & Ownership Model

**Status:** Accepted  
**Date:** 2026-08-30  
**Deciders:** Lead Software Architect & Engineering Team  

---

## Context & Problem Statement

In Sprint 2 of NextDrop, we need to introduce **Customer** and **Restaurant** business domains. On-demand delivery systems often suffer from tight coupling where User identity attributes (passwords, emails, authentication tokens) bleed into customer profiles, or where restaurant management logic trusts client-supplied owner IDs without server-side validation.

We require a clear domain ownership model and resource authorization mechanism that preserves Modular Monolith isolation.

---

## Decision Drivers

1. **Strict Ownership Boundaries:**
   - `Identity` owns `User`, credentials, JWT tokens, refresh tokens, and email verification.
   - `Customer` owns `Customer` profile aggregate, `CustomerAddress` collection, and `CustomerPreferences` value object.
   - `Restaurant` owns `Restaurant` aggregate, `RestaurantBranch` collection, `RestaurantOperatingHours` value objects, `RestaurantDeliveryZone` entities, and `RestaurantStaffMembership` entities.
2. **Identity Cross-Referencing:** Modules reference `UserId` (Identity's stable identifier GUID) directly without duplicating user password/email domain logic or directly referencing Identity entity classes.
3. **Single Active Default Address Protection:** Enforce both domain-level toggle invariants and PostgreSQL partial unique index `CREATE UNIQUE INDEX ON customers.customer_addresses (customer_id) WHERE is_default = true AND is_active = true`.
4. **Resource-Scoped Authorization:** Server-side `RestaurantAuthorizationHandler` verifies `UserId + RestaurantId` relationship dynamically.

---

## Decision Outcome

**Chosen Design:**

1. **Customer Aggregate:** `CustomerAddress` is owned directly inside the `Customer` aggregate root. Address soft-deactivation (`IsActive = false`) preserves order history integrity.
2. **Restaurant Staff Membership:** `RestaurantStaffMembership` roles (`Owner`, `Manager`, `Staff`) represent restaurant-specific operational privileges distinct from platform-level JWT identity roles.
3. **Overnight Operating Hours Evaluation:** `RestaurantOperatingHours` stores `OpenTime` and `CloseTime` as `TimeOnly`. Overnight schedules (e.g. 18:00 to 02:00 where `CloseTime < OpenTime`) are evaluated via `IsOpenAt(TimeOnly localTime)` logic. Timezone is represented as an IANA timezone string (e.g., `"Africa/Cairo"`).
4. **Geographic & Monetary Precision:** `Latitude` / `Longitude` use PostgreSQL `numeric(9,6)`. `DeliveryFee` / `MinimumOrderAmount` use PostgreSQL `numeric(18,2)`.

---

## Consequences

* **Positive:** Complete elimination of cross-module entity leaks; robust security preventing IDOR vulnerabilities; 100% testable domain logic.
* **Negative:** Queries combining customer profile and identity email require application-level join projections.
