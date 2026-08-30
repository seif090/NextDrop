# NextDrop — Search, Discovery & Restaurant Browsing API Specification

## Base URL
`/api/v1/discovery`

---

## Endpoints Summary

| Method | Endpoint | Auth | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/discovery/restaurants` | Public / Anonymous | Search and filter active restaurants |
| `GET` | `/api/v1/discovery/restaurants/{id}` | Public / Anonymous | Get public restaurant details by ID |
| `GET` | `/api/v1/discovery/restaurants/{id}/branches` | Public / Anonymous | Get active branches for a restaurant |
| `GET` | `/api/v1/discovery/restaurants/{id}/menu` | Public / Anonymous | Browse menu items for a restaurant |
| `GET` | `/api/v1/discovery/menu/search` | Public / Anonymous | Global menu item search across categories |

---

## 1. Get Public Restaurants

`GET /api/v1/discovery/restaurants?city=Cairo&district=Maadi&openNow=true&page=1&pageSize=20`

### Response `200 OK`
```json
{
  "items": [
    {
      "id": "1fa85f64-5717-4562-b3fc-2c963f66afa9",
      "name": "Cairo Burgers",
      "description": "Best burgers in Cairo",
      "phoneNumber": "+20123456789",
      "email": "cairoburgers@test.com",
      "status": "Active",
      "branches": [
        {
          "id": "b47c0c1b-9345-4299-81bc-540c95029a1a",
          "restaurantId": "1fa85f64-5717-4562-b3fc-2c963f66afa9",
          "branchName": "Maadi Branch",
          "addressLine": "Road 9",
          "city": "Cairo",
          "district": "Maadi",
          "timezone": "Africa/Cairo",
          "status": "Active",
          "isOpenNow": true,
          "minimumOrderAmount": 50.00,
          "estimatedDeliveryFee": 15.00,
          "estimatedDeliveryTimeMinutes": 30
        }
      ]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```
