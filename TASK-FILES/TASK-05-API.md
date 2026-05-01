# API Endpoints

Base URL: `https://localhost:7124`
Interactive docs: `https://localhost:7124/scalar/v1`
OpenAPI JSON: `https://localhost:7124/openapi/v1.json`

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

### GET /api/organizations
List all organisations.

**Query params:**
- `includeInactive` (bool, default `false`) — include inactive organisations
- `search` (string, optional) — case-insensitive LIKE filter applied to `Name`, `Abbreviation`, and `OrganizationCode`

**Response:** `200 OK` — array of `OrganizationDto`

**Notes:**
- Results are always ordered by `Name`
- `search` is trimmed server-side; passing an empty string is treated as no filter

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

### GET /api/organizations/{organizationId}/fields
List all field definitions for a organization.

**Query params:**
- `includeInactive` (bool, default false)

**Response:** `200 OK` — array of `FieldDefinitionDto` (includes options for dropdown/multiselect)

---

### GET /api/organizations/{organizationId}/fields/{fieldId}
Get a single field definition.

**Response:** `200 OK` — `FieldDefinitionDto`
**Response:** `404 Not Found`

---

### POST /api/organizations/{organizationId}/fields
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
  "regexPattern": null,
  "displayFormat": null
}
```

**Valid fieldType values:** `text`, `number`, `date`, `datetime`, `checkbox`, `dropdown`, `multiselect`, `phone`

**`displayFormat`** — only used when `fieldType` is `phone`. Controls how stored digits are rendered.
Valid values: `"(XXX) XXX-XXXX"`, `"XXX-XXX-XXXX"`, `"XXX.XXX.XXXX"`. Null for all other field types.
Phone values are always stored as digits only (non-digits stripped on import).

**Response:** `201 Created` — `FieldDefinitionDto`
**Response:** `400 Bad Request`
**Response:** `409 Conflict` — fieldKey already exists for this organization

---

### PUT /api/organizations/{organizationId}/fields/{fieldId}
Update a field definition. `fieldKey` cannot be changed.

**Response:** `200 OK` — `FieldDefinitionDto`
**Response:** `404 Not Found`

---

### DELETE /api/organizations/{organizationId}/fields/{fieldId}
Soft-deactivate a field. Existing FieldValues are retained.

**Response:** `204 No Content`
**Response:** `404 Not Found`

---

### PATCH /api/organizations/{organizationId}/fields/reorder
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

## Field Sections

All section routes are scoped under an organisation.

### GET /api/organisations/{organisationId}/sections
List all field sections for the organisation.

**Response:** `200 OK` — array of `FieldSectionDto`
```json
[
  {
    "sectionId": "guid",
    "organisationId": "guid",
    "sectionName": "Personal Information",
    "displayOrder": 1,
    "isActive": true,
    "fields": []
  }
]
```

---

### POST /api/organisations/{organisationId}/sections
Create a new section.

**Body:**
```json
{
  "sectionName": "Employment Details",
  "displayOrder": 2
}
```

**Response:** `201 Created` — `FieldSectionDto`

---

### GET /api/organisations/{organisationId}/sections/{sectionId}
Get a single section.

**Response:** `200 OK` — `FieldSectionDto`
**Response:** `404 Not Found`

---

### PUT /api/organisations/{organisationId}/sections/{sectionId}
Update a section's name or display order.

**Response:** `200 OK` — `FieldSectionDto`
**Response:** `404 Not Found`

---

### PATCH /api/organisations/{organisationId}/sections/{sectionId}/status
Activate or deactivate a section. Fields within the section are unaffected.

**Body:**
```json
{ "isActive": false }
```

**Response:** `204 No Content`

---

### POST /api/organisations/{organisationId}/sections/reorder
Update the display order for multiple sections in one call.

**Body:**
```json
{
  "sections": [
    { "sectionId": "guid", "displayOrder": 1 },
    { "sectionId": "guid", "displayOrder": 2 }
  ]
}
```

**Response:** `204 No Content`

---

### PUT /api/organisations/{organisationId}/sections/{sectionId}/fields
Assign fields to a section (and set their display order within it).
Replaces the section's current field assignments.

**Body:**
```json
{
  "fields": [
    { "fieldDefinitionId": "guid", "displayOrder": 1 },
    { "fieldDefinitionId": "guid", "displayOrder": 2 }
  ]
}
```

**Response:** `204 No Content`

---

### GET /api/organisations/{organisationId}/customers/{customerId}/form-preview
Returns the full form structure for a customer, grouped by section, with each
field's current saved value. Used by the admin Inputs page Form Preview panel.

**Response:** `200 OK` — `CustomerFormPreviewDto`
```json
{
  "customerId": "guid",
  "customerName": "Jane Smith",
  "sections": [
    {
      "sectionId": "guid",
      "sectionName": "Personal Information",
      "fields": [
        {
          "fieldDefinitionId": "guid",
          "fieldKey": "first_name",
          "fieldLabel": "First Name",
          "fieldType": "text",
          "isRequired": true,
          "helpText": null,
          "currentValue": "Jane",
          "options": []
        }
      ]
    }
  ],
  "unassignedFields": []
}
```

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

## Contracts

All contract routes are scoped under an organisation.

### GET /api/organisations/{organisationId}/contracts
List all contracts for the organisation. Active contract first, then by start date descending.

**Query params:**
- `includeInactive` (bool, default false)

**Response:** `200 OK` — array of `ContractDto`

---

### GET /api/organisations/{organisationId}/contracts/{contractId}
Get a single contract.

**Response:** `200 OK` — `ContractDto`
**Response:** `404 Not Found`

---

### POST /api/organisations/{organisationId}/contracts
Create a new contract. If another contract is already active for this org, the request is rejected.

**Body:**
```json
{
  "contractName": "2026 Service Agreement",
  "contractNumber": "SA-2026-001",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "notes": null,
  "createdBy": "admin@example.com"
}
```

**Response:** `201 Created` — `ContractDto`
**Response:** `400 Bad Request`
**Response:** `409 Conflict` — another contract is already active

---

### PUT /api/organisations/{organisationId}/contracts/{contractId}
Update a contract's details.

**Response:** `200 OK` — `ContractDto`
**Response:** `404 Not Found`

---

### PATCH /api/organisations/{organisationId}/contracts/{contractId}/status
Activate or deactivate a contract.

**Body:**
```json
{ "isActive": false, "modifiedBy": "admin@example.com" }
```

**Response:** `204 No Content`
**Response:** `409 Conflict` — trying to activate when another is already active

---

## Marketing Projects

All project routes are scoped under an organisation. Multiple projects
can be active simultaneously under the same organisation.

### GET /api/organisations/{organisationId}/projects
List all marketing projects for the organisation.

**Query params:**
- `includeInactive` (bool, default false)

**Response:** `200 OK` — array of `MarketingProjectDto`

---

### GET /api/organisations/{organisationId}/projects/{projectId}
Get a single project by its INT project ID.

**Response:** `200 OK` — `MarketingProjectDto`
**Response:** `404 Not Found`

---

### POST /api/organisations/{organisationId}/projects
Create a new marketing project. Project ID is auto-assigned starting at 8000.

**Body:**
```json
{
  "contractId": null,
  "projectName": "Spring 2026 Campaign",
  "marketingStartDate": "2026-03-01",
  "marketingEndDate": "2026-06-30",
  "notes": null,
  "createdBy": "admin@example.com"
}
```

**Response:** `201 Created` — `MarketingProjectDto`

---

### PUT /api/organisations/{organisationId}/projects/{projectId}
Update a project.

**Response:** `200 OK` — `MarketingProjectDto`
**Response:** `404 Not Found`

---

### PATCH /api/organisations/{organisationId}/projects/{projectId}/status
Toggle project active/inactive.

**Body:**
```json
{ "isActive": false, "modifiedBy": "admin@example.com" }
```

**Response:** `204 No Content`

---

## Customers

All customer routes are scoped under an organisation.

### GET /api/organisations/{organisationId}/customers
List customers for the organisation.

**Query params:**
- `includeInactive` (bool, default false)
- `page` (int, default 1)
- `pageSize` (int, default 50)

**Response:** `200 OK` — `PagedResult<CustomerDto>`

---

### GET /api/organisations/{organisationId}/customers/{customerId}
Get a single customer.

**Response:** `200 OK` — `CustomerDto`
**Response:** `404 Not Found`

---

### POST /api/organisations/{organisationId}/customers
Create a single customer manually. `CustomerCode` is system-generated.

**Body:**
```json
{
  "firstName": "Jane",
  "lastName": "Smith",
  "middleName": null,
  "originalId": "MBR-00123",
  "email": "jane@example.com"
}
```

**Response:** `201 Created` — `CustomerDto`

---

### PUT /api/organisations/{organisationId}/customers/{customerId}
Update a customer.

**Response:** `200 OK` — `CustomerDto`
**Response:** `404 Not Found`

---

### PATCH /api/organisations/{organisationId}/customers/{customerId}/status
Activate or deactivate a customer.

**Response:** `204 No Content`

---

## Customer Addresses

Address history per customer. Every address change is preserved —
`IsCurrent` marks the live address. `POST` replaces the current address
(retires the old one) rather than updating it in-place.

Melissa address validation is called automatically on every `POST`.
The stub implementation always returns `MelissaValidated = false` until
real credentials are wired in — see `Services/MelissaService.cs`.

### GET /api/customers/{customerId}/addresses
List all addresses for a customer, newest first (full history).

**Response:** `200 OK` — array of `CustomerAddressDto`

---

### GET /api/customers/{customerId}/addresses/current
Get the customer's current address.

**Response:** `200 OK` — `CustomerAddressDto`
**Response:** `404 Not Found` — no address on record

---

### POST /api/customers/{customerId}/addresses
Add a new address. The previous current address is retired (`IsCurrent` set to `0`).
The address is submitted to Melissa for validation before saving — if Melissa confirms
it, the standardised form is stored and `MelissaValidated` is set to `true`.

**Body:**
```json
{
  "addressLine1": "123 Main St",
  "addressLine2": null,
  "city": "Springfield",
  "state": "IL",
  "postalCode": "62701",
  "country": "US"
}
```

**Response:** `201 Created` — `CustomerAddressDto`
**Response:** `404 Not Found` — customer not found

---

### PATCH /api/customers/{customerId}/addresses/{addressId}/confirm
Customer confirms the address is correct. Sets `CustomerConfirmed = true`.

**Response:** `200 OK` — `CustomerAddressDto`
**Response:** `404 Not Found`

---

### CustomerAddressDto
```json
{
  "addressId": "guid",
  "customerId": "guid",
  "addressLine1": "123 Main St",
  "addressLine2": null,
  "city": "Springfield",
  "state": "IL",
  "postalCode": "62701",
  "country": "US",
  "melissaValidated": false,
  "customerConfirmed": false,
  "isCurrent": true,
  "createdUtcDt": "2026-04-30T00:00:00Z",
  "modifiedUtcDt": "2026-04-30T00:00:00Z"
}
```

---

## Dashboard

### GET /api/dashboard/stats
Returns summary statistics across all active organisations.

**Response:** `200 OK`
```json
{
  "totalActiveOrganizations": 12,
  "totalActiveProjects": 8,
  "totalCustomers": 4500,
  "totalVerifiedCustomers": 1200,
  "organisationSummaries": [
    {
      "organisationId": "guid",
      "organisationName": "Acme Corp",
      "totalCustomers": 300,
      "verifiedCustomers": 85,
      "activeProjects": 2
    }
  ],
  "expiringProjects": []
}
```

---

### GET /api/dashboard/expiring-projects
Returns active projects whose `MarketingEndDate` falls within the configured warning window
(default 30 days; set via `DashboardSettings:WarningDaysThreshold` in appsettings).

**Response:** `200 OK` — array of `ExpiringProjectDto`
```json
[
  {
    "projectId": 8001,
    "projectName": "Spring 2026 Campaign",
    "organisationId": "guid",
    "organisationName": "Acme Corp",
    "marketingEndDate": "2026-05-15",
    "daysUntilExpiry": 18
  }
]
```

---

## Import

### POST /api/organisations/{organisationId}/imports
Upload a CSV or Excel file. Parses headers, auto-matches columns, creates an `ImportBatch`.

**Body:** `multipart/form-data`
- `file` — the CSV/XLSX/XLS file
- `uploadedBy` (string)
- `duplicateStrategy` (string, default `skip`) — `skip`, `update`, or `error`

**Response:** `201 Created` — `UploadImportResponseDto`

---

### GET /api/organisations/{organisationId}/imports
List import history. Returns `PagedResult<ImportBatchDto>`.

---

### GET /api/organisations/{organisationId}/imports/{batchId}
Get a single import batch status. Returns `ImportBatchDto`.

---

### GET /api/organisations/{organisationId}/imports/saved-mappings?fingerprint={hash}
Check for saved column mappings. Returns array of `ColumnMatchResultDto` (empty if none).

---

### POST /api/organisations/{organisationId}/imports/{batchId}/mappings
Save column mappings for a batch. Advances status to `preview`. Body: `SaveMappingsRequest`.

**`ColumnMatchResultDto` / `ColumnMappingDto` shape** (used in saved-mappings response and mapping request body):
```json
{
  "csvHeader": "Full Name",
  "csvColumnIndex": 2,
  "destinationTable": "customer",
  "destinationField": null,
  "transformType": "split_full_name",
  "fieldDefinitionId": null,
  "isAutoMatched": false,
  "isRequired": false,
  "savedForReuse": true,
  "displayOrder": 2,
  "outputs": [
    { "outputToken": "FirstName",    "destinationTable": "customer", "destinationField": "FirstName",    "fieldDefinitionId": null, "sortOrder": 1 },
    { "outputToken": "MiddleName",   "destinationTable": "customer", "destinationField": "MiddleName",   "fieldDefinitionId": null, "sortOrder": 2 },
    { "outputToken": "LastName",     "destinationTable": "customer", "destinationField": "LastName",     "fieldDefinitionId": null, "sortOrder": 3 },
    { "outputToken": "Suffix",       "destinationTable": "skip",     "destinationField": null,           "fieldDefinitionId": null, "sortOrder": 4 },
    { "outputToken": "Credentials",  "destinationTable": "skip",     "destinationField": null,           "fieldDefinitionId": null, "sortOrder": 5 }
  ]
}
```

**`destinationTable` values:** `customer` | `customer_address` | `field_value` | `skip`

**`destinationField` by table:**
- `customer` → `FirstName`, `LastName`, `MiddleName`, `MaidenName`, `DateOfBirth`, `Email`, `Phone`, `OriginalId`
- `customer_address` → `AddressLine1`, `AddressLine2`, `City`, `State`, `PostalCode`, `Country`, `AddressType`
- `field_value` → leave `null`; set `fieldDefinitionId` instead
- `skip` → leave `null`

**`transformType` values:** `direct` (1:1 mapping) | `split_full_name` (parses name parts via `FullNameParser`; requires `outputs` array)

---

### POST /api/organisations/{organisationId}/imports/{batchId}/preview
Returns the first 10 rows with mapping applied and per-row validation status. Returns `ImportPreviewDto`.

---

### POST /api/organisations/{organisationId}/imports/{batchId}/execute
Runs the full import asynchronously. Returns `202 Accepted`. Poll batch status for completion.

---

### GET /api/organisations/{organisationId}/imports/{batchId}/errors
Returns failed rows. Returns array of `ImportErrorDto`.

---

## Import Column Staging

### GET /api/organisations/{organisationId}/import-staging
List staged columns. Query param: `status` (optional filter).

### GET /api/organisations/{organisationId}/import-staging/{stagingId}
Get a single staging record.

### PUT /api/organisations/{organisationId}/import-staging/{stagingId}
Resolve or skip a staged column. Body: `ResolveColumnStagingRequest`. Returns `ImportColumnStagingDto`.

### DELETE /api/organisations/{organisationId}/import-staging/{stagingId}
Remove a staging record.

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
