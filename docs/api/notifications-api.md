# NextDrop — Notifications & Real-Time Tracking API Specification

## Base URL
`/api/v1`

---

## Endpoints Summary

| Method | Endpoint | Auth | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/notifications` | Authenticated | Get paged notifications for authenticated user |
| `GET` | `/api/v1/notifications/unread` | Authenticated | Get list of unread notifications |
| `POST` | `/api/v1/notifications/{id}/read` | Owner | Mark notification as read (BOLA protected) |
| `POST` | `/api/v1/notifications/read-all` | Authenticated | Mark all unread notifications read |
| `DELETE` | `/api/v1/notifications/{id}` | Owner | Delete notification (BOLA protected) |
| `GET` | `/api/v1/notifications/preferences` | Authenticated | Get notification preferences |
| `PUT` | `/api/v1/notifications/preferences` | Authenticated | Update notification preferences |
| `POST` | `/api/v1/riders/me/location` | Rider | Update live GPS location (throttled) |
| `WS` | `/hubs/orders` | Authenticated | SignalR Hub for real-time order tracking & rider location |

---

## 1. Get Paged Notifications

`GET /api/v1/notifications?page=1&pageSize=20`

### Response `200 OK`
```json
{
  "items": [
    {
      "id": "1fa85f64-5717-4562-b3fc-2c963f66afa9",
      "userId": "b47c0c1b-9345-4299-81bc-540c95029a1a",
      "type": "OrderPlaced",
      "title": "Order Placed",
      "body": "Your order has been received.",
      "dataJson": "{\"orderId\":\"...\"}",
      "channel": "InApp",
      "priority": "Normal",
      "status": "Unread",
      "createdAtUtc": "2026-08-30T14:30:00Z",
      "readAtUtc": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

---

## 2. SignalR Hub Usage (`/hubs/orders`)

### Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/orders", { accessTokenFactory: () => jwtToken })
    .build();

await connection.start();

// Subscribe to active order tracking
await connection.invoke("SubscribeToOrder", orderId);

// Listen for real-time status updates
connection.on("OrderStatusChanged", (data) => {
    console.log("Order update received:", data);
});

// Listen for live rider GPS location updates
connection.on("RiderLocationUpdated", (location) => {
    console.log("Rider location:", location.latitude, location.longitude);
});
```
