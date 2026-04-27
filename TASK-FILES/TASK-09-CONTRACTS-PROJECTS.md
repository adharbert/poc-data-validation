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
            └── linked to a Contract (optional)
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
| `StartDate` | DATE — when the contract begins |
| `EndDate` | DATE, nullable — open-ended contracts have NULL |
| `Notes` | Free text for admin context |

---

## Marketing Projects

### Business Rules

| Rule | Detail |
|---|---|
| Multiple active | No uniqueness constraint on active projects per org |
| Project ID | INT IDENTITY(8000,1) — sequential from 8000 |
| Contract link | Optional FK to Contracts — a project may exist without a contract |
| End-date warning | Dashboard warns when `MarketingEndDate` is within `DashboardSettings:WarningDaysThreshold` days |

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

## API Routes Summary

```
Contracts:
  GET    /api/organisations/{orgId}/contracts
  GET    /api/organisations/{orgId}/contracts/{contractId}
  POST   /api/organisations/{orgId}/contracts
  PUT    /api/organisations/{orgId}/contracts/{contractId}
  PATCH  /api/organisations/{orgId}/contracts/{contractId}/status

Marketing Projects:
  GET    /api/organisations/{orgId}/projects
  GET    /api/organisations/{orgId}/projects/{projectId}
  POST   /api/organisations/{orgId}/projects
  PUT    /api/organisations/{orgId}/projects/{projectId}
  PATCH  /api/organisations/{orgId}/projects/{projectId}/status

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
