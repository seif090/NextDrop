# NextDrop — Payment & Transactional Checkout API Specification

## Base URL
`/api/v1`

---

## Endpoints Summary

| Method | Endpoint | Auth | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/checkout` | Customer | Atomically checkout cart, calculate catalog prices server-side, create Order & Payment |
| `GET` | `/api/v1/payments/{paymentId}` | Owner | Get payment details (BOLA protected) |
| `POST` | `/api/v1/payments/{paymentId}/confirm` | Owner | Confirm/capture payment and mark Order as Paid |
| `POST` | `/api/v1/payments/{paymentId}/cancel` | Owner | Cancel pending payment |
| `POST` | `/api/v1/payments/{paymentId}/refund` | Owner | Create partial or full refund for captured payment |
| `POST` | `/api/v1/payments/webhooks/{provider}` | Anonymous | Public payment webhook endpoint with signature verification & replay protection |

---

## 1. Transactional Checkout

`POST /api/v1/checkout`

### Request Headers
```http
Authorization: Bearer <JWT>
Idempotency-Key: 7b8c9d10-e11f-1213-1415-161718192021
Content-Type: application/json
```

### Request Body
```json
{
  "cartId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "deliveryAddressId": "8fa85f64-5717-4562-b3fc-2c963f66afa7"
}
```

### Response `200 OK`
```json
{
  "orderId": "9fa85f64-5717-4562-b3fc-2c963f66afa8",
  "orderNumber": "ND-2026-8A9B0C",
  "paymentId": "1fa85f64-5717-4562-b3fc-2c963f66afa9",
  "totalAmount": 55.00,
  "paymentStatus": "Pending",
  "orderStatus": "PendingPayment"
}
```

---

## 2. Confirm Payment

`POST /api/v1/payments/{paymentId}/confirm`

### Request Headers
```http
Authorization: Bearer <JWT>
Idempotency-Key: 8c9d10e1-1f12-1314-1516-171819202122
```

### Response `200 OK`
```json
{
  "id": "1fa85f64-5717-4562-b3fc-2c963f66afa9",
  "orderId": "9fa85f64-5717-4562-b3fc-2c963f66afa8",
  "userId": "b47c0c1b-9345-4299-81bc-540c95029a1a",
  "amount": 55.00,
  "currency": "USD",
  "status": "Captured",
  "provider": "FakeProvider",
  "providerPaymentId": "fake_pay_1fa85f6457174562b3fc2c963f66afa9",
  "createdAtUtc": "2026-08-30T14:15:00Z",
  "capturedAtUtc": "2026-08-30T14:15:05Z"
}
```

---

## 3. Create Refund

`POST /api/v1/payments/{paymentId}/refund`

### Request Body
```json
{
  "amount": 20.00,
  "reason": "Item arrived damaged"
}
```

### Response `200 OK`
```json
{
  "id": "2fa85f64-5717-4562-b3fc-2c963f66afb0",
  "paymentId": "1fa85f64-5717-4562-b3fc-2c963f66afa9",
  "orderId": "9fa85f64-5717-4562-b3fc-2c963f66afa8",
  "userId": "b47c0c1b-9345-4299-81bc-540c95029a1a",
  "amount": 20.00,
  "currency": "USD",
  "status": "Completed",
  "reason": "Item arrived damaged",
  "providerRefundId": "fake_ref_2fa85f6457174562b3fc2c963f66afb0",
  "createdAtUtc": "2026-08-30T14:16:00Z",
  "completedAtUtc": "2026-08-30T14:16:01Z"
}
```

---

## 4. Payment Webhook

`POST /api/v1/payments/webhooks/{provider}`

### Headers
```http
X-Webhook-Signature: valid_sig_123
X-Webhook-Event-Id: evt_unique_9999
Content-Type: application/json
```

### Response `200 OK`
```json
{
  "status": "processed"
}
```
