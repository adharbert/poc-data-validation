# What's Next

Everything not yet built, roughly in build order.

---

## Completed ✓

The following have been fully built (API + Admin SPA):

| Feature | Notes |
|---|---|
| Organisations CRUD | List, create, edit, activate/deactivate |
| Organisation search | Server-side `?search=` on `GET /api/organizations`; debounced input on OrganizationsPage; client-side filter on DashboardPage org table |
| Organisation Detail Page | Per-org stats, contracts + projects timelines, validation progress, nav tiles |
| Field Definitions (Inputs) | Full CRUD, type-aware modal (incl. phone + display format), options management |
| Phone field type | Stored as digits only; rendered via configurable `DisplayFormat` |
| Phone input masking | Live `(XXX) XXX-XXXX` formatting while typing; strips to digits on save |
| Field Sections | Create/edit/reorder/assign, drag-and-drop in Inputs page |
| Form Preview | Admin selects customer → reads form with live values |
| Customers | AG Grid list with client-side search/sort/filter/pagination; row click opens detail |
| Customer Detail Page | Per-customer view: core info, emails, phones, addresses, field values |
| Customer OriginalId dedup | Import deduplicates on OriginalId first, then Email, within same org |
| Contracts | Per-org, single active constraint enforced (list + create/edit UI only) |
| Contract Amendments | API complete — no Admin SPA UI yet |
| Contract Line Items | API complete — no Admin SPA UI yet |
| Contract Documents | API complete (upload/download/delete) — no Admin SPA UI yet |
| Marketing Projects | Per-org, multiple active allowed (list + create/edit UI; projectType dropdown not yet on modal) |
| Dashboard | Global stat cards, org comparison chart, expiring projects list, org search filter |
| Import (5-step wizard) | Upload CSV/Excel, column mapping, value mapping, preview, execute |
| Import Staging | Resolve unmatched columns post-import |
| Breadcrumb navigation | All sub-pages show Organisations → Org name → Page breadcrumb |
| Consistent date formatting | All dates display as MM/dd/yyyy via shared `fmtDate()` util |
| Serilog sinks | Console always on; DB sinks (InformationLogs/ErrorLogs) toggled by config |
| SVG logo | PCI logo in sidebar; collapses to icon when sidebar is collapsed |
| Unit test suite | `POC.CustomerValidation.Test` — 142 xUnit tests, all 10 controllers, ≥ 90% coverage |
| Customer Addresses | `CustomerAddresses` table (temporal), full address history per customer, `IsCurrent` flag |
| Azure Blob Storage provisioning | `IOrganizationStorageService` / `AzureBlobOrganizationStorageService` — container created on org create; Azurite for local dev (`UseDevelopmentStorage=true`) |
| SFTP project folders | `ProvisionProjectFolderAsync` — `imports/{projectId}/.keep` placeholder created on `MarketingProject` create; gives each project an isolated SFTP drop-zone |
| Async import execution | `ImportProcessorBackgroundService` + `ImportQueue` (`Channel<Guid>`) — execute endpoint returns 202 immediately; worker drains queue and runs import; SignalR push notifies UI on completion |
| SignalR import status | `ImportHub` with group `import:{batchId}`; server pushes `ImportStatusChanged` on complete/fail; client joins group on execute step mount, leaves on unmount; 30s polling fallback |
| Import error review | `StepResults` shows errors and warnings auto-loaded; "Fix Column Mappings" (reset to pending), "Fix Value Aliases" (reset to preview), "Retry" buttons on failed batches |
| Import History review | "Review" button on completed batches reopens the results step for any historical batch |
| Event Grid blob webhook | `POST /api/internal/blob-events` — handles `Microsoft.Storage.BlobCreated`; auto-creates `ImportBatch` for SFTP-dropped files within seconds of upload |
| Blob import polling (safety net) | `BlobImportPollingService` — scans `imports/{projectId}/` folders; 30-second interval in Development (Azurite), 1-hour fallback in Production; idempotent via `Notes = sftp:{blobPath}` |
| Request body size fix | `RequestLoggingMiddleware` skips body buffering for `multipart/form-data`; upload endpoint allows files up to 50 MB |
| Melissa stub | `IMelissaService` + stub wired into address create flow — sets `MelissaValidated` when real API is connected |
| Number display fix | `fmtNumber()` in `src/utils/dates.js` — strips trailing zeros (`42.00` → `42`, `3.14` → `3.14`) |

---

## Priority 1 — Segmentations (Phase 3 — in progress)

**Database:** ✓ `Segmentations` and `CustomerSegmentations` tables created and registered in SSDT project.

**Still needed:**

### API
- Add `ProjectType` column to `MarketingProjects` table (SSDT + CHECK constraint)
- `SegmentationsController` — CRUD under `/api/organisations/{orgId}/projects/{projectId}/segmentations`
- `POST .../from-field` — field-split endpoint (with `dryRun` preview)
- `POST .../import` — segmentation import file endpoint (CSV/Excel, `OriginalId` matching)
- `GET /api/organisations/{orgId}/customers/{customerId}/segmentations` — customer's segments
- Update `POST /api/organisations/{orgId}/projects` to require and validate `projectType`

### Admin SPA
- **Project create/edit modal** — add required `ProjectType` dropdown (9 options)
- **Project detail page** — add "Segmentations" tab
  - List segments with customer count per segment
  - "Build from field" button → field picker → preview table → confirm
  - "Import file" button → upload CSV, map columns
- **Customer detail page** — segmentation chip list

---

## Priority 2 — Analytics, Campaigns & Canonical Standardization (Phase 4)

Full design captured in [TASK-10-ANALYTICS-CAMPAIGNS.md](TASK-10-ANALYTICS-CAMPAIGNS.md).

Not yet started. Build order:
1. Canonical taxonomy tables + seed data + admin mapping UI
2. Customer scores import (CSV keyed on `CustomerCode`)
3. Org campaign policy + project channel rules
4. Vendor export endpoints (Five9, Iterable, mailing)
5. Standardised cross-org reports

---

## ~~Priority 3 — Customer Detail Page (Admin SPA)~~ ✓ Completed

**Route:** `/organizations/:organizationId/customers/:customerId`

Implemented in `CustomerDetailPage.jsx`. Shows:
- Customer core information (name, email, phone, DOB, client ID, customer code)
- All email addresses (`GET /organisations/{orgId}/customers/{customerId}/emails`)
- All phone numbers (`GET /organisations/{orgId}/customers/{customerId}/phones`)
- Full address history (`GET /api/customers/{customerId}/addresses`) with Current / Confirmed badges
- All field key/value pairs (`GET /api/customers/{customerId}/values`) with confirmed status
- Edit and Activate/Deactivate actions in the page header

**Still not built:**
- Change history table (field value audit trail — endpoint exists at `GET /api/customers/{id}/values/history`)
- Address create form (addresses are read-only on this page)

---

## Priority 3 — Customer Validation Portal

Separate React app for customers to review and confirm their data.

**Location:** `ClientPortal/customer-portal/` — Vite app not yet scaffolded
**Port:** 5174

### API endpoints (not yet built)
```
GET  /api/portal/customers/{identifier}         Load customer + their field values
PUT  /api/portal/values/{valueId}/confirm       Confirm a field as correct
PUT  /api/portal/values/{valueId}/flag          Flag a field with a note
POST /api/portal/sessions                       Start a validation session
PUT  /api/portal/sessions/{sessionId}/complete  Mark session as complete
```

### Pages needed
1. `CustomerLookupPage` — enter email or customer code
2. `ValidationFormPage` — dynamic form driven by FieldDefinitions
   - Renders correct widget per FieldType (see table below)
   - Confirm / Flag button per field
   - Progress bar (confirmed / total)
   - Fields grouped by FieldSection if assigned
3. `CompletePage` — summary of confirmed vs flagged, thank-you message

### Field rendering by type
| FieldType | Widget |
|---|---|
| `text` | `<input type="text">` |
| `number` | `<input type="number">` with min/max |
| `date` | `<input type="date">` |
| `boolean` | Toggle / checkbox |
| `dropdown` | `<select>` populated from FieldOptions |
| `multiselect` | Checkbox list from FieldOptions |
| `phone` | Read-only formatted display (digits stored, `displayFormat` controls rendering) |

### Scaffold steps
```bash
cd ClientPortal
npm create vite@latest customer-portal -- --template react
cd customer-portal
npm install bootstrap react-router-dom @tanstack/react-query react-hook-form sass
```
Copy `src/api/client.js` fetch wrapper from admin SPA — same pattern applies.

---

## Priority 4 — Authentication

Both portals currently have no authentication.
`Microsoft.Identity.Web` is installed in the API but `[Authorize]` is not applied.

### When adding auth:
- Apply `[Authorize]` to all admin controllers
- Admin SPA: reinstate MSAL (`@azure/msal-browser`, `@azure/msal-react`) and restore
  the Bearer token request interceptor in `src/api/client.js`
- Customer portal: magic-link authentication (passwordless)
  - Customer enters email → receives one-time link → portal exchanges for session token
  - Token expires after 24 hours / single use
  - Needs: token generation service, email sender, session table in DB

### Azure AD app registrations needed
See `docs/AZURE_AD_SETUP.md` for full step-by-step.

Two registrations:
1. `CustomerValidation-API` — exposes scopes + app roles
2. `CustomerValidation-AdminSPA` — public client, SPA redirect URIs

App Roles (define on API registration):
- `SuperAdmin` — manages all organisations
- `OrgAdmin` — manages their assigned organisation only
- `Reviewer` — read-only access

---

## Known Issues / Technical Debt

| Item | Notes |
|---|---|
| Import row throughput | Row-by-row Dapper inserts (~6 DB calls per row). 600k-row files complete correctly but slowly. Future: `SqlBulkCopy` batch insert to cut ~3.6M calls down to ~600. |
| Import XLSX memory | ClosedXML loads the entire workbook into memory. For 600k+ row XLSX files this can use 1–3 GB RAM. Future: migrate to ExcelDataReader or OpenXml SAX streaming reader. |
| Abbreviation not required on create | Org can be created without Abbreviation. Import will fail later — warn on org form. |
| Melissa not connected | `MelissaService` is a stub — always returns `IsValid=false`. Wire up real Melissa REST API when credentials available. |
| No address UI | Admin SPA has no address create form on the customer detail page yet — addresses are read-only. |
| Azure Storage connection string blank | `appsettings.json` has `AzureStorage:ConnectionString` empty — fill in the real Azure Blob Storage connection string before deploying to production. |

---

## Migration Scripts Run Order

```
1. (base schema — initial tables)
2. Post-Deployment/01_SeedData.sql                          -- test customers
3. Post-Deployment/02_SeedDataFieldOptions_States.sql       -- US states
4. Post-Deployment/03_SeedDataFieldOptions_HighestDegree.sql
5. Post-Deployment/04_Migration_ImportTables.sql            -- import + staging tables
6. Post-Deployment/05_Contract_3CFDCADA.sql                 -- seed contract for ADX org
7. Post-Deployment/06_MarketingProject_ADX.sql              -- seed project for ADX org
8. scripts/Migrations/Migration_003_FieldDefinitions_Phone.sql  -- adds DisplayFormat column
```
