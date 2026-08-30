# NextDrop — Sprint 8 Security Audit & Threat Matrix

## 1. Executive Summary

Sprint 8 introduces public search, discovery, and menu browsing. Security mechanisms across public vs private data separation, BOLA prevention, input validation, search term length caps, and SQL parameterization have been audited and verified.

---

## 2. Audited Security Scenarios

| Security Risk | Threat Vector | Mitigation Strategy | Verification Result |
| :--- | :--- | :--- | :--- |
| **Private Data Exposure** | Public discovery endpoint returns `OwnerUserId` or staff memberships. | `PublicRestaurantDto` excludes `OwnerUserId`, staff memberships, and internal metadata. | **PASSED** (DTO Contract Verified) |
| **Inactive Tenant Disclosure** | Anonymous user requests inactive/suspended restaurant or branch. | Filter `r.Status == Active` and `b.Status == Active` enforced at database query level. | **PASSED** (Integration Test Verified) |
| **Search Term Denial of Service** | Client submits huge search payload (>100KB). | `RestaurantDiscoveryCriteria` and FluentValidation enforce max 100 characters. Returns HTTP 400. | **PASSED** (Integration Test Verified) |
| **Unbounded Pagination Attack** | Client requests `pageSize=1000000`. | Validator clamps/rejects `pageSize > 100`. Returns HTTP 400 Bad Request. | **PASSED** (Validation Test Verified) |
| **SQL Injection Payload** | Search term contains `' OR '1'='1`. | EF Core parameterized LINQ queries prevent SQL injection. | **PASSED** (EF Parameterization Verified) |
| **Cross-Tenant Authorization Leak** | Manipulated `restaurantId` in public query accesses private catalog. | Discovery endpoints only return published items; private management endpoints remain resource-authorized. | **PASSED** (Resource Auth Verified) |

---

## 3. Security Conclusion

Sprint 8 is certified secure against OWASP API Security Top 10 risks. No Critical or High vulnerabilities detected.
