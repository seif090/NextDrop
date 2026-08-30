# Rider & Delivery API Specification — NextDrop Sprint 5

**Base Path:** `/api/v1`  
**Authentication:** JWT Bearer Token (`Authorization: Bearer <token>`)  

---

## 1. Rider Endpoints

* **`GET /api/v1/riders/me`**
  * **Auth:** Required (Rider)
  * **Response (200 OK):** `RiderDto`

* **`POST /api/v1/riders/me/availability`**
  * **Auth:** Required (Rider)
  * **Body:** `{"availabilityStatus": "Available" | "Offline"}`
  * **Response (200 OK):** `RiderDto`

* **`POST /api/v1/riders/me/location`**
  * **Auth:** Required (Rider)
  * **Body:** `{"latitude": 30.044, "longitude": 31.235, "accuracy": 10.0, "heading": 180.0, "speed": 15.0}`
  * **Response (204 No Content)**

---

## 2. Delivery Endpoints

* **`GET /api/v1/deliveries/{deliveryId}`**
  * **Auth:** Required (Customer owner or Assigned Rider)
  * **Response (200 OK):** `DeliveryDto`

* **`POST /api/v1/deliveries/{deliveryId}/accept`**
  * **Auth:** Required (Rider)
  * **Header:** `Idempotency-Key: <unique-uuid>`
  * **Response (200 OK):** `DeliveryDto`

* **`POST /api/v1/deliveries/{deliveryId}/reject`**
  * **Auth:** Required (Rider)
  * **Body:** `{"reason": "Vehicle issue"}`
  * **Response (204 No Content)**

* **`POST /api/v1/deliveries/{deliveryId}/arrive`**
  * **Auth:** Required (Assigned Rider)
  * **Response (204 No Content)**

* **`POST /api/v1/deliveries/{deliveryId}/pickup`**
  * **Auth:** Required (Assigned Rider)
  * **Header:** `Idempotency-Key: <unique-uuid>`
  * **Response (204 No Content)**

* **`POST /api/v1/deliveries/{deliveryId}/start`**
  * **Auth:** Required (Assigned Rider)
  * **Response (204 No Content)**

* **`POST /api/v1/deliveries/{deliveryId}/complete`**
  * **Auth:** Required (Assigned Rider)
  * **Header:** `Idempotency-Key: <unique-uuid>`
  * **Response (204 No Content)**

* **`POST /api/v1/deliveries/{deliveryId}/fail`**
  * **Auth:** Required (Assigned Rider)
  * **Header:** `Idempotency-Key: <unique-uuid>`
  * **Body:** `{"reason": "Customer unreachable"}`
  * **Response (204 No Content)**
