# Catalog & Menu Management API Specification — NextDrop Sprint 3

**Base Path:** `/api/v1`  
**Authentication:** JWT Bearer Token (`Authorization: Bearer <token>`)  

---

## 1. Management Endpoints (Owner & Manager)

### Catalog Lifecycle

* **`POST /api/v1/restaurants/{restaurantId}/catalog`**
  * **Auth:** Required (Owner/Manager)
  * **Body:** `{"name": "Main Menu", "description": "Summer Special"}`
  * **Response (201 Created):** `CatalogDto`

* **`GET /api/v1/catalogs/{catalogId}`**
  * **Auth:** Required (Owner/Manager)
  * **Response (200 OK):** `CatalogDto`

* **`POST /api/v1/catalogs/{catalogId}/publish`**
  * **Auth:** Required (Owner/Manager)
  * **Response (200 OK):** Updated `CatalogDto` (Status = `Published`, Version incremented)

### Category & Item Management

* **`POST /api/v1/catalogs/{catalogId}/categories`**
  * **Auth:** Required (Owner/Manager)
  * **Body:** `{"name": "Burgers", "description": "Juicy burgers", "displayOrder": 0}`
  * **Response (201 Created):** `CategoryDto`

* **`POST /api/v1/categories/{categoryId}/items`**
  * **Auth:** Required (Owner/Manager)
  * **Body:** `{"name": "Smokey Burger", "description": "Bacon & Cheese", "basePrice": 120.00, "displayOrder": 0}`
  * **Response (201 Created):** `MenuItemDto`

* **`PUT /api/v1/menu-items/{menuItemId}/price`**
  * **Auth:** Required (Owner/Manager)
  * **Body:** `{"newPrice": 135.00}`
  * **Response (204 No Content)**

---

## 2. Public Read Endpoint (Consumers)

* **`GET /api/v1/restaurants/{restaurantId}/catalog`**
  * **Auth:** Anonymous allowed
  * **Response (200 OK):**
    ```json
    {
      "restaurantId": "r1a2b3c4-0000-0000-0000-000000000001",
      "name": "Main Menu",
      "description": "Summer Special",
      "version": 2,
      "categories": [
        {
          "id": "cat12345-0000-0000-0000-000000000001",
          "name": "Burgers",
          "description": "Juicy burgers",
          "displayOrder": 0,
          "menuItems": [
            {
              "id": "m1a2b3c4-0000-0000-0000-000000000001",
              "name": "Smokey Burger",
              "description": "Bacon & Cheese",
              "basePrice": 135.00,
              "isAvailable": true,
              "variants": [],
              "modifierGroups": []
            }
          ]
        }
      ]
    }
    ```
