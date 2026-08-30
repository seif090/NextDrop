# ADR-003: Notifications, Real-Time Order Tracking & SignalR Architecture

## Status
Approved

## Context
NextDrop requires multi-channel notification processing, inbox deduplication for integration events, real-time order tracking, and live rider location updates without creating tight module coupling or exposing security vulnerabilities.

## Decision
1. **Notifications Bounded Context:**
   - Create `NextDrop.Modules.Notifications` following Modular Monolith boundaries.
   - Domain logic must remain independent of ASP.NET Core, SignalR, Redis, or EF Core.

2. **Inbox Integration Event Deduplication:**
   - Store processed integration events in `notifications.processed_integration_events` with unique index `(ConsumerName, EventId)`.
   - Replayed RabbitMQ/Outbox events return `Result.Success()` without generating duplicate notifications.

3. **SignalR Order Tracking Security:**
   - Implement `OrderTrackingHub` at `/hubs/orders`.
   - Authenticate connections using JWT bearer tokens.
   - Enforce server-side authorization before joining `order:{orderId}` groups.

4. **Ephemeral Live Rider Location:**
   - Store live GPS location ephemerally in Redis (`rider:{riderId}:location`).
   - Do NOT store raw location streams in PostgreSQL tables.
   - Broadcast live updates to authorized `order:{orderId}` subscribers.

## Consequences
- Guarantees idempotent notification creation and secure real-time tracking.
- Maintains clean module isolation verified by NetArchTest rules.
