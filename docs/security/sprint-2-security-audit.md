# Sprint 2 Security Audit — NextDrop

**Date:** 2026-08-30  
**Auditor:** Senior Security Architect  
**Status:** PASSED (Zero Critical/High Vulnerabilities)  

---

## Executive Summary

A comprehensive security audit of Sprint 2 (Customer & Restaurant Foundation) was performed across authorization boundaries, broken object-level authorization (IDOR) resistance, input validation, state machine integrity, and automated security test scenarios.

---

## Security Verification Scenarios (Section 50 Mandate)

| Scenario | Risk Description | Tested Behavior | Status |
| :--- | :--- | :--- | :---: |
| **Scenario 1** | Customer A attempts to access/deactivate Customer B's address | Controller derives `UserId` from authenticated JWT claims. Requesting B's address returns HTTP `404 Not Found` (Resource Hiding Policy). | **PASS** |
| **Scenario 2** | Restaurant Owner A attempts to modify Owner B's restaurant | `RestaurantAuthorizationHandler` checks `UserId + RestaurantId` association server-side. Returns HTTP `403 Forbidden`. | **PASS** |
| **Scenario 3** | Restaurant Staff attempts an Owner-only operation (status update/add staff) | Domain method `UserHasRole(userId, RestaurantStaffRole.Owner)` checks staff role. Returns HTTP `403 Forbidden`. | **PASS** |
| **Scenario 4** | Anonymous user attempts restaurant management API | ASP.NET Core JWT authentication middleware rejects unauthenticated requests with HTTP `401 Unauthorized`. | **PASS** |

---

## Security Matrix

1. **Broken Object-Level Authorization (BOLA / IDOR):**
   Client-supplied `UserId`, `CustomerId`, or `OwnerUserId` in request payloads are strictly ignored. All user identities are derived from validated JWT token claims (`sub`).
2. **Single Active Default Address Protection:**
   PostgreSQL partial unique index `is_default = true AND is_active = true` guarantees that database race conditions cannot produce multiple default addresses.
3. **State Machine Tampering:**
   Restaurant status transitions are strictly controlled in domain logic. Attempts to transition `Archived` $\rightarrow$ `Active` return HTTP `409 Conflict`.
4. **Data Over-Fetching:**
   Public discovery endpoints return lean `RestaurantSummaryDto` instances, preventing exposure of staff memberships, owner IDs, or internal branch notes.
