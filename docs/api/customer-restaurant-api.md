# Customer & Restaurant API Specification — NextDrop Sprint 2

**Base Path:** `/api/v1`  
**Authentication:** JWT Bearer Token (`Authorization: Bearer <token>`)  

---

## 1. Customer Endpoints (`/api/v1/customers`)

### Profile Management

* **`GET /api/v1/customers/me`**
  * **Auth:** Required (Customer)
  * **Response (200 OK):**
    ```json
    {
      "id": "c1a2b3c4-0000-0000-0000-000000000001",
      "userId": "u1a2b3c4-0000-0000-0000-000000000001",
      "firstName": "John",
      "lastName": "Doe",
      "phoneNumber": "+1234567890",
      "preferences": {
        "preferredLanguage": "en",
        "preferredCurrency": "USD",
        "allowMarketingNotifications": true,
        "allowOrderNotifications": true
      },
      "createdAtUtc": "2026-08-30T12:00:00Z"
    }
    ```

* **`PUT /api/v1/customers/me`**
  * **Auth:** Required
  * **Body:**
    ```json
    {
      "firstName": "John",
      "lastName": "Doe",
      "phoneNumber": "+1234567890"
    }
    ```
  * **Response (200 OK):** Returns updated `CustomerDto`.

### Address Management

* **`GET /api/v1/customers/me/addresses`**
  * **Auth:** Required
  * **Response (200 OK):** Array of `CustomerAddressDto`.

* **`POST /api/v1/customers/me/addresses`**
  * **Auth:** Required
  * **Body:**
    ```json
    {
      "label": "Home",
      "recipientName": "John Doe",
      "phoneNumber": "+1234567890",
      "addressLine1": "123 Main Street",
      "addressLine2": "Apt 4B",
      "city": "Cairo",
      "district": "Maadi",
      "buildingNumber": "12",
      "floor": "4",
      "apartment": "4B",
      "latitude": 30.0444,
      "longitude": 31.2357,
      "makeDefault": true
    }
    ```
  * **Response (201 Created):** Returns created `CustomerAddressDto`.

* **`POST /api/v1/customers/me/addresses/{id}/set-default`**
  * **Auth:** Required
  * **Response (204 No Content):** Successfully updated default address.

* **`DELETE /api/v1/customers/me/addresses/{id}`**
  * **Auth:** Required
  * **Response (204 No Content):** Soft-deactivates target address.

---

## 2. Restaurant Endpoints (`/api/v1/restaurants`)

### Discovery (Public)

* **`GET /api/v1/restaurants?page=1&pageSize=10&city=Cairo`**
  * **Auth:** Anonymous allowed
  * **Response (200 OK):** Paged list of active restaurants (`PagedRestaurantResponse`).

* **`GET /api/v1/restaurants/{id}`**
  * **Auth:** Anonymous allowed
  * **Response (200 OK):** `RestaurantDto`.

### Management (Owner & Staff)

* **`POST /api/v1/restaurants`**
  * **Auth:** Required
  * **Body:**
    ```json
    {
      "name": "Burger King",
      "description": "Home of the Whopper",
      "phoneNumber": "+123456789",
      "email": "owner@burgerking.com"
    }
    ```
  * **Response (201 Created):** Returns created `RestaurantDto`.

* **`PUT /api/v1/restaurants/{id}/status`**
  * **Auth:** Required (Owner only)
  * **Body:** `{"status": 2}` (Active=2, TemporarilyClosed=3, Suspended=4, Archived=5)
  * **Response (204 No Content)**

* **`POST /api/v1/restaurants/{id}/branches`**
  * **Auth:** Required (Owner/Manager)
  * **Body:**
    ```json
    {
      "name": "Maadi Branch",
      "phoneNumber": "+123456789",
      "addressLine1": "Road 9",
      "city": "Cairo",
      "district": "Maadi",
      "latitude": 29.9602,
      "longitude": 31.2569,
      "timezone": "Africa/Cairo"
    }
    ```
  * **Response (201 Created)**

* **`PUT /api/v1/restaurants/{id}/branches/{branchId}/operating-hours`**
  * **Auth:** Required (Owner/Manager)
  * **Body:**
    ```json
    [
      { "dayOfWeek": "Friday", "openTime": "18:00", "closeTime": "02:00", "isClosed": false }
    ]
    ```
  * **Response (204 No Content)**

* **`POST /api/v1/restaurants/{id}/branches/{branchId}/delivery-zones`**
  * **Auth:** Required (Owner/Manager)
  * **Body:**
    ```json
    {
      "name": "Maadi Central",
      "deliveryFee": 25.00,
      "minimumOrderAmount": 100.00,
      "estimatedDeliveryMinutes": 30
    }
    ```
  * **Response (201 Created)**

* **`POST /api/v1/restaurants/{id}/staff`**
  * **Auth:** Required (Owner only)
  * **Body:** `{"targetUserId": "...", "role": 2}` (Owner=1, Manager=2, Staff=3)
  * **Response (200 OK)**
