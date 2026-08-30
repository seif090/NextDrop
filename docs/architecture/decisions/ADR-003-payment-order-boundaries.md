# ADR-003: Payment & Transactional Checkout Architectural Boundaries

## Status
Approved

## Context
NextDrop requires a financial transactional ordering system supporting carts, checkout, payments, refunds, and webhooks. Financial integrity mandates that orders must never be created without an accompanying payment record, client-supplied monetary amounts must never be trusted, and payment processing must be idempotent and concurrency-safe.

## Decision
1. **Module Separation:**
   - Implement `NextDrop.Modules.Payments` as an independent module.
   - Do NOT merge Payments into Orders or SharedKernel.

2. **Atomic Checkout Transaction:**
   - Orchestrate checkout in `CheckoutCommandHandler` inside a single DbContext transaction.
   - Resolve prices directly from the Catalog domain server-side.
   - Atomically insert `Order`, `OrderItems`, `Payment`, and `OutboxMessages` while deleting `Cart`.

3. **Payment State Machine & Refund Rules:**
   - Maintain explicit state machines on `Payment` and `Refund`.
   - Prevent over-refunds by verifying `totalRefunds <= CapturedAmount`.
   - Require `Idempotency-Key` headers on state-changing API requests.

4. **Webhook Replay Protection:**
   - Store incoming webhooks in `payments.webhook_events` with unique constraint `(Provider, ProviderEventId)`.

## Consequences
- Guarantees financial consistency and prevents price tampering or double-charging.
- Preserves clean modular monolith boundaries verified by NetArchTest rules.
