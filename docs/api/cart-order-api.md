# Cart, Checkout & Order API Specification — NextDrop Sprint 4

**Base Path:** `/api/v1`  
**Authentication:** JWT Bearer Token (`Authorization: Bearer <token>`)  

---

## 1. Cart Endpoints

* **`POST /api/v1/carts`**
  * **Auth:** Required (Customer)
  * **Body:** `{"restaurantId": "...", "restaurantBranchId": "..."}`
  * **Response (201 Created):** `CartDto`

* **`GET /api/v1/carts`**
  * **Auth:** Required (Customer)
  * **Response (200 OK):** `CartDto`

* **`POST /api/v1/carts/{cartId}/items`**
  * **Auth:** Required (Customer)
  * **Body:** `{"menuItemId": "...", "variantId": null, "quantity": 2, "notes": "Extra crispy"}`
  * **Response (200 OK):** `CartDto`

* **`POST /api/v1/carts/{cartId}/checkout`**
  * **Auth:** Required (Customer)
  * **Header:** `Idempotency-Key: <unique-uuid>`
  * **Body:** `{"deliveryAddressId": "..."}`
  * **Response (200 OK):** `CheckoutResultDto`

---

## 2. Order Endpoints

* **`GET /api/v1/orders`**
  * **Auth:** Required (Customer)
  * **Params:** `page=1`, `pageSize=10`
  * **Response (200 OK):** `PagedOrdersDto`

* **`GET /api/v1/orders/{orderId}`**
  * **Auth:** Required (Customer owner or Restaurant staff)
  * **Response (200 OK):** `OrderDto`

* **`POST /api/v1/orders/{orderId}/cancel`**
  * **Auth:** Required (Customer owner or Restaurant staff)
  * **Body:** `{"reason": "Customer changed mind"}`
  * **Response (204 No Content)**
