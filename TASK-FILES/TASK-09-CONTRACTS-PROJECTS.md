# Contracts, Marketing Projects & Import Staging

Added in phase 2 to support multi-tenant project lifecycle management
and persistent import column resolution.

---

## Business Model Overview

```
Organizations
    │
    ├── Contracts       (overall org engagement; one active at a time)
    │
    └── MarketingProjects  (the "project" concept; many can be active)
            ├── linked to a Contract (optional)
            └── Segmentations  (named customer groupings scoped to a project)
                    └── CustomerSegmentations  (many-to-many: customers ↔ segments)
```

- A **Contract** represents the formal agreement with an organization.
  Only one contract per organization may be `IsActive = 1` at a time
  (enforced by filtered unique index). Previous contracts are retained
  for history.

- A **MarketingProject** is the operational "project" that
  admins and the dashboard track. Many projects can be active under
  the same organization simultaneously. Each project has a
  `MarketingStartDate` and optional `MarketingEndDate`.

- **Project IDs** are INT identity starting at **8000** to distinguish
  them visually from other numeric IDs in the system.

---

## Contracts

### Business Rules

| Rule | Detail |
|---|---|
| One active per org | Filtered unique index `UQ_Contracts_ActivePerOrg` on `(OrganizationId) WHERE IsActive = 1` |
| History retained | Deactivated contracts remain in the table; never deleted |
| New end dates | Create a new contract — do NOT update the end date of the active one |
| Deactivation | PATCH status endpoint; deactivating auto-allows a new one to become active |

### Status Flow

```
active (IsActive=1)  ──PATCH status──►  inactive (IsActive=0)
```

### Key Columns

| Column | Notes |
|---|---|
| `ContractNumber` | External reference (e.g. from CRM). Optional. |
| `StartDate` | DATE — when the contract begins. Never changed by amendments. |
| `OriginalEndDate` | DATE, nullable — set at creation, never changed. |
| `EndDate` | DATE, nullable — current effective end date; updated when an amendment extends the contract. |
| `OriginalCost` | DECIMAL(18,2), nullable — base contracted amount; never changed. |
| `TotalCost` | DECIMAL(18,2), nullable — `OriginalCost + SUM(AmendmentCost)`; updated when amendments add cost. |
| `Notes` | Free text for admin context |

---

## Contract Amendments

Amendments may only be applied to a contract that `IsActive = 1 AND EndDate >= today`.
An expired contract cannot be amended.

Each amendment can:
- **Extend the end date** — `NewEndDate` replaces `Contracts.EndDate`
- **Add cost** — `AmendmentCost` is added to `Contracts.TotalCost` (additive, not replacement)

At least one of the two must be supplied.

**Example cost rollup:**
```
Original contract:   $100,000   (OriginalCost = TotalCost = 100,000)
Amendment #1:        +$30,000   (AmendmentCost = 30,000 → TotalCost = 130,000)
Amendment #2:        +$15,000   (AmendmentCost = 15,000 → TotalCost = 145,000)
Contract header now shows TotalCost = $145,000
```

### Amendment Columns

| Column | Notes |
|---|---|
| `AmendmentNumber` | Sequential per contract: 1, 2, 3… |
| `AmendmentDate` | Date the amendment was signed |
| `PreviousEndDate` | Snapshot of `EndDate` before this amendment — audit trail |
| `NewEndDate` | Replacement end date. NULL = not changing. |
| `AmendmentCost` | Additive cost increase. NULL = not changing. Must be > 0. |
| `DocumentFileName` / `DocumentPath` | Uploaded amendment document |

---

## Marketing Projects

### Business Rules

| Rule | Detail |
|---|---|
| Multiple active | No uniqueness constraint on active projects per org |
| Project ID | INT IDENTITY(8000,1) — sequential from 8000 |
| Contract link | Optional FK to Contracts — a project may exist without a contract |
| Project type | Required on create — must select one of the 9 allowed types (see below) |
| End-date warning | Dashboard warns when `MarketingEndDate` is within `DashboardSettings:WarningDaysThreshold` days |

### Project Types

| Value | Display Label |
|---|---|
| `public_university` | Public University |
| `private_university` | Private University |
| `public_high_school` | Public High School |
| `private_high_school` | Private High School |
| `fraternities` | Fraternities |
| `sororities` | Sororities |
| `military` | Military |
| `general` | General |
| `story_cause` | Story Cause |

### Dashboard Warning

Controlled by `appsettings.json`:

```json
"DashboardSettings": {
  "WarningDaysThreshold": 30
}
```

The `GET /api/dashboard/expiring-projects` endpoint returns all active
projects whose `MarketingEndDate` falls within the configured window.

---

## Import Column Staging

Persistent table (`ImportColumnStaging`) that stores CSV/Excel column
headers that could not be auto-matched during an import upload.

### Why Persistent

ETL files are uploaded repeatedly. If a column header cannot be matched
on the first upload, an admin or ETL team member can resolve it once —
and the resolution is applied automatically on subsequent uploads with
the same header.

### Staging Record Lifecycle

```
Upload encounters unknown header
         │
         ▼
ImportColumnStaging row created (Status = 'unmatched')
         │
         ├──► Admin visits Staging UI
         │         │
         │         ├── Maps to existing field  →  Status = 'resolved', MappingType/FieldDefinitionId set
         │         ├── Maps to customer field   →  Status = 'resolved', MappingType = 'customer_field'
         │         └── Skips/ignores           →  Status = 'skipped'
         │
         └──► Next upload with same header
                   If resolved  →  auto-applied as a mapping suggestion
                   If skipped   →  auto-marked as skip
                   If unmatched →  SeenCount incremented, LastSeenAt updated
```

### Key Columns

| Column | Notes |
|---|---|
| `HeaderNormalized` | Lowercased, trimmed version used for matching + unique constraint |
| `SeenCount` | How many import uploads have included this column |
| `Status` | `unmatched`, `resolved`, `skipped` |
| `MappingType` | `customer_field`, `field_definition`, or NULL |

### Unique Constraint

`UQ_ImportColumnStaging_OrgHeader` — `(OrganizationId, HeaderNormalized)`

One staging record per org per unique header name. Duplicate uploads
increment `SeenCount` and update `LastSeenAt` rather than creating
duplicate rows.

---

## Customer OriginalId

Added `OriginalId NVARCHAR(100) NULL` to the `Customers` table.

**Purpose:** Stores the client's own identifier for a customer (e.g. a
member ID, account number, or any opaque string). This allows the ETL
team to map a column from the import file to `OriginalId` and enables
exact lookups using the client's system ID.

**Import mapping:** `OriginalId` is a valid value for `CustomerFieldName`
in `ImportColumnMappings`. It can be mapped from a CSV column just like
`FirstName`, `LastName`, etc.

**Index:** `IX_Customers_OriginalId` on `(OrganizationId, OriginalId)
WHERE OriginalId IS NOT NULL` for efficient lookup during imports.

---

## Segmentations

Named groupings of customers scoped to a marketing project. A customer may
belong to one or many segments within a project.

### Business Rules

| Rule | Detail |
|---|---|
| Scoped to project | `SegmentationKey` is unique per project — `UQ_Segmentations_ProjectKey (ProjectId, SegmentationKey)` |
| One customer per segment | `UQ_CustomerSegmentations_CustomerSeg (CustomerId, SegmentationId)` — no duplicate assignments |
| Two creation modes | **Field split** (from existing imported field) or **Import file** (separate CSV keyed on `OriginalId`) |
| Cross-file matching | When a separate segmentation file is loaded, customers are matched by `(OrganizationId, OriginalId)` |
| Auto-create segments | Both modes create missing `Segmentation` rows on the fly |

### Field Split Flow

1. Admin selects a `FieldDefinition` already loaded for the project.
2. API reads all distinct `FieldValues` for that field across the org's customers.
3. One `Segmentation` row is created per distinct value (key = normalised value, name = raw value).
4. `CustomerSegmentations` rows are bulk-inserted with `Source = 'field_split'`.

### Import File Flow

1. Admin uploads a CSV/Excel with at minimum two mapped columns: `OriginalId` and segmentation key.
2. API resolves each `OriginalId` → `CustomerId` via `(OrganizationId, OriginalId)` index.
3. Missing `Segmentation` rows are created for any new keys seen in the file.
4. `CustomerSegmentations` rows are inserted with `Source = 'import_file'`.
5. Unmatched `OriginalId` values are reported as errors — they do not abort the batch.

---

## API Routes Summary

```
Contracts:
  GET    /api/organisations/{orgId}/contracts
  GET    /api/organisations/{orgId}/contracts/{contractId}
  POST   /api/organisations/{orgId}/contracts
  PUT    /api/organisations/{orgId}/contracts/{contractId}
  PATCH  /api/organisations/{orgId}/contracts/{contractId}/status

Contract Amendments:
  GET    /api/organisations/{orgId}/contracts/{contractId}/amendments
  POST   /api/organisations/{orgId}/contracts/{contractId}/amendments

Contract Line Items:
  GET    /api/organisations/{orgId}/contracts/{contractId}/line-items
  POST   /api/organisations/{orgId}/contracts/{contractId}/line-items
  PUT    /api/organisations/{orgId}/contracts/{contractId}/line-items/{lineItemId}
  DELETE /api/organisations/{orgId}/contracts/{contractId}/line-items/{lineItemId}

Contract Documents:
  GET    /api/organisations/{orgId}/contracts/{contractId}/documents
  POST   /api/organisations/{orgId}/contracts/{contractId}/documents     (multipart/form-data)
  GET    /api/organisations/{orgId}/contracts/{contractId}/documents/{docId}   (download/open)
  DELETE /api/organisations/{orgId}/contracts/{contractId}/documents/{docId}

Marketing Projects:
  GET    /api/organisations/{orgId}/projects
  GET    /api/organisations/{orgId}/projects/{projectId}
  POST   /api/organisations/{orgId}/projects              (projectType required)
  PUT    /api/organisations/{orgId}/projects/{projectId}
  PATCH  /api/organisations/{orgId}/projects/{projectId}/status

Segmentations:
  GET    /api/organisations/{orgId}/projects/{projectId}/segmentations
  POST   /api/organisations/{orgId}/projects/{projectId}/segmentations
  PUT    /api/organisations/{orgId}/projects/{projectId}/segmentations/{segId}
  PATCH  /api/organisations/{orgId}/projects/{projectId}/segmentations/{segId}/status
  POST   /api/organisations/{orgId}/projects/{projectId}/segmentations/from-field
         Body: { fieldDefinitionId, dryRun }  — preview or execute field-split
  POST   /api/organisations/{orgId}/projects/{projectId}/segmentations/import
         Multipart: file + column mapping (originalId col, segmentation col)

Customer Segmentation Assignments:
  GET    /api/organisations/{orgId}/customers/{customerId}/segmentations

Customers:
  GET    /api/organisations/{orgId}/customers
  GET    /api/organisations/{orgId}/customers/{customerId}
  POST   /api/organisations/{orgId}/customers
  PUT    /api/organisations/{orgId}/customers/{customerId}
  PATCH  /api/organisations/{orgId}/customers/{customerId}/status

Import:
  POST   /api/organisations/{orgId}/imports              (upload file)
  GET    /api/organisations/{orgId}/imports              (history)
  GET    /api/organisations/{orgId}/imports/{batchId}    (batch status)
  POST   /api/organisations/{orgId}/imports/{batchId}/mappings
  GET    /api/organisations/{orgId}/imports/saved-mappings?fingerprint=...
  POST   /api/organisations/{orgId}/imports/{batchId}/preview
  POST   /api/organisations/{orgId}/imports/{batchId}/execute
  GET    /api/organisations/{orgId}/imports/{batchId}/errors

Import Column Staging:
  GET    /api/organisations/{orgId}/import-staging
  GET    /api/organisations/{orgId}/import-staging/{stagingId}
  PUT    /api/organisations/{orgId}/import-staging/{stagingId}
  DELETE /api/organisations/{orgId}/import-staging/{stagingId}

Dashboard:
  GET    /api/dashboard/stats
  GET    /api/dashboard/expiring-projects
```
