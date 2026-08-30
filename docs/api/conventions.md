# NextDrop API Conventions & Standards

**Version:** 1.0  
**Base Path:** `/api/v1`  

---

## 1. Request & Response Conventions

* **API Versioning:** All endpoints are prefixed with `/api/v1/...`.
* **JSON Naming:** Standard camelCase formatting for request and response JSON properties.
* **Async & Cancellation:** All controller actions are `async Task<IActionResult>` and accept `CancellationToken`.
* **DTO Isolation:** Controllers never accept or return EF Core entity classes directly. Explicit DTOs (`RegisterUserResponse`, `UserDto`, `AuthResponse`) are used.

---

## 2. Idempotency Key Specification

For state-changing operations (such as registration and future order placements):

* Header: `Idempotency-Key: <unique-uuid-or-key>`
* Behavior:
  * **Same key + Same payload:** Replays original response (StatusCode, ContentType, Body).
  * **Same key + Different payload:** Returns `HTTP 409 Conflict` (RFC 7807 ProblemDetails).

---

## 3. Correlation Identifier

Every HTTP request is assigned a Correlation ID:

* Request Header: `X-Correlation-ID` (Optional on request; generated automatically if missing).
* Response Header: `X-Correlation-ID` (Always returned).
* ProblemDetails: Embedded under `problemDetails.extensions.correlationId`.

---

## 4. Error Format (RFC 7807 ProblemDetails)

All error responses return `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "User.EmailNotUnique",
  "status": 409,
  "detail": "The specified email address is already registered.",
  "instance": "/api/v1/auth/register",
  "correlationId": "f45c7229-b3f1-4ebd-9ee9-399dee3de7cd"
}
```

---

## 5. HTTP Status Codes

| Code | Usage |
| :--- | :--- |
| **200 OK** | Successful retrieval, login, refresh, or status update. |
| **201 Created** | Successful creation (e.g. `POST /api/v1/auth/register`). |
| **400 Bad Request** | Validation failure or malformed payload. |
| **401 Unauthorized** | Invalid credentials, expired access token, or revoked refresh token. |
| **403 Forbidden** | Account suspended or insufficient policy privileges. |
| **404 Not Found** | Target resource does not exist. |
| **409 Conflict** | Email already registered or Idempotency payload mismatch. |
| **429 Too Many Requests** | Rate limit exceeded. |
| **500 Internal Server Error** | Unexpected internal failure (details sanitized). |
