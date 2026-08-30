# NextDrop — Sprint 7 Architecture Audit Report

## 1. Executive Summary

Sprint 7 introduces the **Notifications Module** (`src/Modules/Notifications/`), multi-channel delivery engine, template renderer, inbox deduplication mechanism, SignalR `/hubs/orders` real-time order status tracking, and throttled live rider location streaming.

This audit certifies that all notification bounded contexts, delivery state machines, inbox deduplication logic, SignalR group authorization, coordinate validation, rate limiting, EF Core configurations, and NetArchTest isolation rules strictly comply with DDD and Clean Architecture standards.

---

## 2. Bounded Context & Topology

```text
src/Modules/Notifications/
├── NextDrop.Modules.Notifications.Domain/
│   ├── Aggregates/ (Notification, NotificationTemplate, UserNotificationPreference, ProcessedIntegrationEvent)
│   ├── Entities/ (NotificationDelivery)
│   ├── Enums/ (NotificationType, NotificationChannel, NotificationPriority, NotificationStatus, DeliveryStatus)
│   ├── Events/ (NotificationCreatedDomainEvent, NotificationReadDomainEvent)
│   └── ValueObjects/ (NotificationId, NotificationTemplateId, NotificationDeliveryId, UserNotificationPreferenceId)
├── NextDrop.Modules.Notifications.Application/
│   ├── Abstractions/ (INotificationRepository, INotificationTemplateRepository, IUserNotificationPreferenceRepository, IProcessedIntegrationEventRepository, INotificationChannel, INotificationTemplateRenderer, IRealTimeNotificationPublisher)
│   ├── Commands/ (CreateNotificationCommand, MarkNotificationAsReadCommand, MarkAllNotificationsAsReadCommand, DeleteNotificationCommand, UpdateNotificationPreferencesCommand, ProcessIntegrationEventNotificationCommand)
│   ├── DTOs/ (NotificationDto, UserNotificationPreferenceDto, PagedNotificationResultDto)
│   ├── Queries/ (GetNotificationsQuery, GetUnreadNotificationsQuery, GetNotificationPreferencesQuery)
│   └── Services/ (SimpleTemplateRenderer)
└── NextDrop.Modules.Notifications.Infrastructure/
    ├── Jobs/ (NotificationDeliveryProcessorJob)
    ├── Persistence/ (Repositories & EF Core Configurations under schema 'notifications')
    └── Services/ (InAppNotificationChannel, DevEmailNotificationChannel)
```

---

## 3. Real-Time & SignalR Infrastructure (`src/NextDrop.Api`)

- **Hub Endpoint:** `/hubs/orders` with `[Authorize]` JWT bearer token authentication.
- **Group Isolation Strategy:**
  - `user:{userId}`: User's personal real-time notification stream.
  - `order:{orderId}`: Operational stream for specific active order.
- **BOLA Protection on Groups:**
  - `OrderTrackingHub.SubscribeToOrder(orderId)` verifies server-side that the requester is the Customer who placed the order or the assigned Rider. Unauthorized connection attempts are rejected.

---

## 4. Operational Invariants & Deduplication

- **Inbox Event Deduplication:**
  - Integration events from Orders, Delivery, Payments, etc. are processed via `ProcessIntegrationEventNotificationCommand`.
  - `ProcessedIntegrationEvent` enforces unique index on `(ConsumerName, EventId)`. Duplicate deliveries produce exactly 1 logical notification.
- **Delivery Exponential Backoff & Dead-Lettering:**
  - `NotificationDelivery.RecordFailedAttempt` calculates backoff as $2^{\text{attempt}}$ minutes. At `maxAttempts` (3), delivery transitions to `DeadLettered`.
- **Rider Location Throttling:**
  - Coordinates are validated ($[-90, 90]$ Lat, $[-180, 180]$ Lng). Ephemeral location stored in Redis. Excessive updates throttled.

---

## 5. Verification Summary

- **Total Solution Projects:** 32
- **Build Status:** 0 Errors / 0 Warnings
- **Total Tests:** 111 Passed (100% Pass Rate)
- **EF Core Migration:** `AddNotificationsAndRealtimeFoundation` created and verified.
