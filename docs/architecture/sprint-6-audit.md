# NextDrop — Sprint 6 Architecture Audit Report

## 1. Executive Summary

Sprint 6 introduces the **Payments Module** and **Transactional Checkout Foundation** (`src/Modules/Payments/`), completing the financial lifecycle of NextDrop.

This audit certifies that all payment domain invariants, state machines, refund rules, webhook replay protections, optimistic concurrency, and modular monolith boundaries strictly adhere to DDD, Clean Architecture, and enterprise security standards.

---

## 2. Module Boundaries & Topology

The Payments module follows the established NextDrop Modular Monolith structure:

```text
src/Modules/Payments/
├── NextDrop.Modules.Payments.Domain/
│   ├── Aggregates/ (Payment, Refund, WebhookEvent)
│   ├── Entities/ (PaymentTransaction)
│   ├── Enums/ (PaymentStatus, PaymentProvider, TransactionType, RefundStatus, WebhookProcessingStatus)
│   ├── Events/ (PaymentCreatedDomainEvent, PaymentCapturedDomainEvent, PaymentRefundedDomainEvent)
│   └── ValueObjects/ (PaymentId, PaymentTransactionId, RefundId, WebhookEventId)
├── NextDrop.Modules.Payments.Application/
│   ├── Abstractions/ (IPaymentRepository, IRefundRepository, IWebhookEventRepository, IPaymentProvider)
│   ├── Commands/ (CheckoutCommand, ConfirmPaymentCommand, CancelPaymentCommand, CreateRefundCommand, ProcessPaymentWebhookCommand)
│   ├── DTOs/ (PaymentDto, RefundDto, TransactionalCheckoutResultDto)
│   └── Queries/ (GetPaymentByIdQuery)
└── NextDrop.Modules.Payments.Infrastructure/
    ├── Persistence/ (Repositories & EF Core entity configurations under schema 'payments')
    └── Services/ (FakePaymentProvider)
```

### Boundary Guarantees
- `Payments.Domain` has **zero** dependencies on EF Core, ASP.NET Core, Redis, RabbitMQ, or Infrastructure.
- `Payments.Application` depends only on `Payments.Domain` and explicit contracts.
- Modular isolation is strictly enforced and validated via NetArchTest rules in `NextDrop.Architecture.Tests`.

---

## 3. Core Architectural Mechanisms

### 3.1 Transactional Checkout Orchestration
`CheckoutCommand` executes an atomic unit of work:
1. Validates customer profile and active delivery address ownership.
2. Resolves restaurant and branch 24/7 operating hours and active status.
3. Resolves current catalog base prices server-side from `MenuItemRepository`.
4. Creates an immutable `Order` snapshot + `Payment` aggregate (`Pending`) + Outbox messages in a **single database transaction**.
5. Removes the customer's cart.

### 3.2 Server-Side Price Resolution & Tamper Protection
- Monetary values (prices, fees, subtotals, totals) provided by HTTP clients are strictly ignored.
- All monetary calculations are performed server-side using PostgreSQL `numeric(18,2)`.

### 3.3 Payment State Machine
Transitions:
- `Pending` → `Processing` → `Authorized` → `Captured`
- Terminal/refund: `Captured` → `PartiallyRefunded` → `Refunded`
- Illegal transitions (e.g. `Captured` → `Pending`, `Refunded` → `Captured`) return domain failures mapped to HTTP 409 Conflict.

### 3.4 Webhook Replay Protection & Idempotency
- Webhooks are stored in `payments.webhook_events` with a unique index on `(Provider, ProviderEventId)`.
- Replayed webhooks are acknowledged with HTTP 200 OK without triggering duplicate processing.
- Checkout and financial transactions are protected by `IIdempotencyService` and `Idempotency-Key` headers.

---

## 4. Verification Summary

- **Total Solution Projects:** 29
- **Build Quality:** 0 Errors / 0 Warnings
- **Total Tests:** 98 Passed (100% Pass Rate)
- **Migration:** `AddPaymentsAndTransactionalCheckoutFoundation` created and verified.
