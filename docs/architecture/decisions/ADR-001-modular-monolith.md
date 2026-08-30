# ADR-001: Adoption of Modular Monolith Architecture Pattern

**Status:** Accepted  
**Date:** 2026-08-30  
**Deciders:** Lead Software Architect & Engineering Team  

---

## Context & Problem Statement

NextDrop is an on-demand food and goods delivery marketplace. Distributed delivery platforms frequently suffer from microservice sprawl during early development stages, leading to network latency overhead, complex distributed transactions (saga management), non-deterministic testing environments, and high operational costs.

We need an architectural pattern that guarantees high developer velocity, testability, and strict module isolation while preserving a simple deployment model for Sprint 1.

---

## Decision Drivers

1. **Domain Boundaries:** Future domains (Identity, Customer, Restaurant, Catalog, Order, Delivery, Rider, Payment) require explicit boundaries.
2. **Operational Simplicity:** Single unit of deployment in early sprints simplifies local dev and CI/CD pipelines.
3. **Future Extraction Path:** If high scale demands extracting an individual domain into an independent microservice later, module separation must make extraction frictionless.
4. **Performance & Reliability:** In-process method calls eliminate network latency for early intra-system communications.

---

## Considered Options

1. **Distributed Microservices:** Separate repos or containers for Identity, Orders, Deliveries, etc.
2. **Traditional Layered Monolith:** Single project with loose folder structures.
3. **Modular Monolith (Chosen):** Clean physical project boundaries per module (`Modules/Identity`, `NextDrop.SharedKernel`, `NextDrop.Infrastructure`, `NextDrop.Api`).

---

## Decision Outcome

**Chosen Option:** **Modular Monolith**.

### Justification:
* **Strict Physical Boundaries:** Enforced using `NetArchTest.Rules` architecture tests, preventing unauthorized cross-module references.
* **Shared Kernel Isolation:** `NextDrop.SharedKernel` contains only shared domain primitives without infrastructure dependencies.
* **Transactional Reliability:** Enables single PostgreSQL database transactions for business state + Outbox message persisting.
* **Extraction Readiness:** Extracting `Modules/Identity` into a standalone microservice in the future requires only wrapping its application handlers with an independent gRPC/HTTP host.

---

## Consequences

* **Positive:** Fast build/test feedback loops, simplified local Docker Compose infrastructure, zero distributed transaction overhead.
* **Negative:** Requires continuous vigilance via architecture tests to prevent developers from bypassing module abstractions.
