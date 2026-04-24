# API Endpoints

Base URL: `https://localhost:7017`
Interactive docs: `https://localhost:7017/scalar/v1`
OpenAPI JSON: `https://localhost:7017/openapi/v1.json`

All endpoints return `application/json`.
Error responses follow the shape: `{ code: string, message: string }`.

---

## Authentication

**Current status: NOT active in POC.**
Azure AD (`Microsoft.Identity.Web`) is installed and configured but
`[Authorize]` attributes are not yet applied to controllers.
All endpoints are currently open.

When auth is activated, every request will need:
```
Authorization: Bearer <Azure AD JWT token>
```

---

## Organisations

### GET /api/organisations
List all organisations.

**Query params:**
- `includeInactive` (bool, default false)

**Response:** `200 OK` — array of `OrganizationDto`

---

### GET /api/organisations/{id}
Get a single organisation by ID.

**Response:** `200 OK` — `OrganizationDto`
**Response:** `404 Not Found`

---

### POST /api/organisations
Create a new organisation.

**Body:**
```json
{
  "organizationName": "Acme Corporation",
  "organizationCode": "ACME-001",
  "filingName": null,
  "marketingName": null,
  "abbreviation": "ACME",
  "website": null,
  "phone": null,
  "companyEmail": null
}
```

**Response:** `201 Created` — `OrganizationDto`
**Response:** `400 Bad Request` — validation errors
**Response:** `409 Conflict` — code already exists

---

### PUT /api/organisations/{id}
Update an organisation.

**Body:** `UpdateOrganizationRequest` (same fields as POST)

**Response:** `200 OK` — `OrganizationDto`
**Response:** `404 Not Found`

---

### PATCH /api/organisations/{id}/status
Activate or deactivate an organisation.

**Body:**
```json
{
  "isActive": false,
  "modifiedBy": "admin@example.com"
}
```

**Response:** `200 OK`
**Response:** `404 Not Found`

---

## Field Definitions

### GET /api/clients/{clientId}/fields
List all field definitions for a client.

**Query params:**
- `includeInactive` (bool, default false)

**Response:** `200 OK` — array of `FieldDefinitionDto` (includes options for dropdown/multiselect)

---

### GET /api/clients/{clientId}/fields/{fieldId}
Get a single field definition.

**Response:** `200 OK` — `FieldDefinitionDto`
**Response:** `404 Not Found`

---

### POST /api/clients/{clientId}/fields
Create a new field definition.

**Body:**
```json
{
  "fieldKey": "highest_degree",
  "fieldLabel": "Highest Degree",
  "fieldType": "dropdown",
  "sectionId": null,
  "placeholderText": "Select your degree",
  "helpText": null,
  "isRequired": false,
  "displayOrder": 3,
  "minValue": null,
  "maxValue": null,
  "minLength": null,
  "maxLength": null,
  "regexPattern": null
}
```

**Valid fieldType values:** `text`, `number`, `date`, `datetime`, `checkbox`, `dropdown`, `multiselect`

**Response:** `201 Created` — `FieldDefinitionDto`
**Response:** `400 Bad Request`
**Response:** `409 Conflict` — fieldKey already exists for this client

---

### PUT /api/clients/{clientId}/fields/{fieldId}
Update a field definition. `fieldKey` cannot be changed.

**Response:** `200 OK` — `FieldDefinitionDto`
**Response:** `404 Not Found`

---

### DELETE /api/clients/{clientId}/fields/{fieldId}
Soft-deactivate a field. Existing FieldValues are retained.

**Response:** `204 No Content`
**Response:** `404 Not Found`

---

### PATCH /api/clients/{clientId}/fields/reorder
Update display order for multiple fields at once.

**Body:**
```json
[
  { "fieldId": "guid", "displayOrder": 1 },
  { "fieldId": "guid", "displayOrder": 2 }
]
```

**Response:** `204 No Content`

---

## Field Options

### GET /api/fields/{fieldId}/options
List options for a dropdown or multiselect field.

**Response:** `200 OK` — array of `FieldOptionDto`

---

### POST /api/fields/{fieldId}/options
Add a single option.

**Body:**
```json
{
  "optionKey": "bach",
  "optionLabel": "Bachelor's degree",
  "displayOrder": 4
}
```

**Response:** `201 Created` — `FieldOptionDto`

---

### PUT /api/fields/{fieldId}/options/{optionId}
Update a single option.

**Response:** `200 OK` — `FieldOptionDto`
**Response:** `404 Not Found`

---

### DELETE /api/fields/{fieldId}/options/{optionId}
Delete a single option.

**Response:** `204 No Content`
**Response:** `404 Not Found`

---

### PUT /api/fields/{fieldId}/options/bulk
Replace ALL options for a field in one call.
Existing options not in the payload are deactivated.

**Body:**
```json
{
  "options": [
    { "optionKey": "none",  "optionLabel": "No formal qualification", "displayOrder": 1 },
    { "optionKey": "bach",  "optionLabel": "Bachelor's degree",       "displayOrder": 4 }
  ]
}
```

**Response:** `204 No Content`

---

## Field Values (admin read-only)

### GET /api/customers/{customerId}/values
All field values for a customer.

**Response:** `200 OK` — array of `FieldValueDto` including `displayValue`
(coalesced from the typed columns for easy rendering)

---

### GET /api/customers/{customerId}/values/history
Full change history for a customer across all fields. Paginated.

**Query params:**
- `page` (int, default 1)
- `pageSize` (int, default 50)

**Response:** `200 OK` — array of `FieldValueHistoryDto`

---

### GET /api/customers/{customerId}/values/{fieldId}/history
Change history for a single field on a customer.

**Response:** `200 OK` — array of `FieldValueHistoryDto`

---

## Import (planned — not yet built)

```
POST /api/import/{orgId}/upload           Parse file headers + row count, create batch
GET  /api/import/{orgId}/mappings         Load saved mappings for auto-match
POST /api/import/{orgId}/mappings         Save / update column mappings
POST /api/import/{batchId}/preview        Apply mapping to first 10 rows
POST /api/import/{batchId}/execute        Run the full import
GET  /api/import/{orgId}/batches          Import history list
GET  /api/import/{batchId}/errors         Failed rows with reasons
```

---

## Customer Portal (planned — not yet built)

```
GET  /api/portal/customers/{identifier}        Load customer + their field values
PUT  /api/portal/values/{valueId}/confirm      Confirm a field as correct
PUT  /api/portal/values/{valueId}/flag         Flag a field with a note
POST /api/portal/sessions                      Start a validation session
PUT  /api/portal/sessions/{sessionId}/complete Mark session as complete
```

---

## Standard DTO Shapes

### OrganizationDto
```json
{
  "organizationId": "guid",
  "organizationName": "Acme Corporation",
  "organizationCode": "01ARZ3NDEKTSV4RR",
  "filingName": null,
  "marketingName": null,
  "abbreviation": "ACME",
  "website": null,
  "phone": null,
  "companyEmail": null,
  "isActive": true,
  "createdAt": "2024-11-01T00:00:00Z",
  "createdBy": "admin",
  "updatedAt": "2024-11-01T00:00:00Z",
  "modifiedBy": null
}
```

### FieldValueDto
```json
{
  "valueId": "guid",
  "customerId": "guid",
  "fieldId": "guid",
  "fieldLabel": "Highest Degree",
  "fieldType": "dropdown",
  "valueText": "bach",
  "valueNumber": null,
  "valueDate": null,
  "valueDatetime": null,
  "valueBoolean": null,
  "displayValue": "bach",
  "confirmedAt": "2025-01-15T10:30:00Z",
  "confirmedBy": "customer",
  "flaggedAt": null,
  "flagNote": null
}
```

### ApiError
```json
{
  "code": "NOT_FOUND",
  "message": "Field abc123 not found."
}
```

**Error codes:**
| Code | HTTP Status | When |
|---|---|---|
| `NOT_FOUND` | 404 | Resource doesn't exist |
| `CONFLICT` | 409 | Duplicate key or invalid state |
| `BAD_REQUEST` | 400 | Argument validation failure |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `INTERNAL_ERROR` | 500 | Unexpected server error |

---

## Health Check

### GET /health
Returns `200 OK` with SQL Server connectivity status.

```json
{
  "status": "Healthy",
  "entries": {
    "sqlserver": { "status": "Healthy" }
  }
}
```
