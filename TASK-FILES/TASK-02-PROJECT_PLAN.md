# Project Plan

Phased build plan. Completed phases are marked ✓.

---

## Phase 1 — Core Admin Features ✓ COMPLETE

**Goal:** Full admin SPA so staff can configure organisations, inputs, and view customers.

### 1a — Organisations ✓
- `GET/POST/PUT/PATCH /api/organisations` — full CRUD
- `OrganizationsPage.jsx` — table, create/edit modal, activate/deactivate

### 1b — Field Definitions & Sections (Inputs) ✓
- Fields API: `GET/POST/PUT/PATCH /api/organizations/{orgId}/fields`
- Field Options API: `GET/POST /api/fields/{fieldId}/options`
- Sections API: `GET/POST/PUT/PATCH /api/organisations/{orgId}/sections` + reorder + assign-fields
- `InputsPage.jsx` — sections and fields on one page, drag-and-drop reorder,
  two-way assignment, field options modal, Form Preview panel
- Route: `/organizations/:organizationId/inputs`

### 1c — Customers ✓
- `GET/POST/PUT/PATCH /api/organisations/{orgId}/customers`
- `CustomersPage.jsx` — paginated table, create/edit/activate/deactivate
- Route: `/organizations/:organizationId/customers`

### 1d — Contracts & Marketing Projects ✓
- Contracts API: `GET/POST/PUT/PATCH /api/organisations/{orgId}/contracts`
- Projects API: `GET/POST/PUT/PATCH /api/organisations/{orgId}/projects`
- Managed via modals within org context

### 1e — Dashboard ✓
- `GET /api/dashboard/stats` + `GET /api/dashboard/expiring-projects`
- `DashboardPage.jsx` — stat cards, org summary table, expiring projects list
- Route: `/dashboard`

---

## Phase 2 — Import Feature ✓ COMPLETE

**Goal:** Allow ETL teams to upload CSV/Excel files and map columns to field definitions.

**Design reference:** `IMPORT.md` for full flow, value mapping rules, and saved mapping reuse.

### Database ✓
- `ImportBatches`, `ImportColumnMappings`, `ImportValueMappings`,
  `ImportErrors`, `SavedColumnMappings`, `ImportColumnStaging` — all created

### NuGet packages ✓
- `CsvHelper 33.0.1` installed
- `ClosedXML 0.104.2` installed

### API ✓
- `POST /api/organisations/{orgId}/imports` — upload, parse, auto-match
- `GET  /api/organisations/{orgId}/imports` — list batches
- `GET  /api/organisations/{orgId}/imports/{batchId}` — batch status
- `GET  /api/organisations/{orgId}/imports/saved-mappings?fingerprint=` — reuse mappings
- `POST /api/organisations/{orgId}/imports/{batchId}/mappings` — save mappings
- `POST /api/organisations/{orgId}/imports/{batchId}/preview` — validate first 10 rows
- `POST /api/organisations/{orgId}/imports/{batchId}/execute` — run import
- `GET  /api/organisations/{orgId}/imports/{batchId}/errors` — error report
- Import Staging CRUD: `GET/PUT/DELETE /api/organisations/{orgId}/import-staging`

### Admin SPA ✓
- `ImportPage.jsx` — 5-step wizard (upload → map columns → map values → preview → execute)
  - Route: `/organizations/:organizationId/import`
- `ImportStagingPage.jsx` — review and resolve unmatched columns
  - Route: `/organizations/:organizationId/import-staging`

---

## Phase 3 — Customer Validation Portal 🔲 NOT STARTED

**Goal:** Separate React app for customers to review and confirm their field values.

**Location:** `ClientPortal/customer-portal/` (Vite app not yet scaffolded)
**Port:** 5174

### 3a — API endpoints (not yet built)
```
GET  /api/portal/customers/{identifier}
PUT  /api/portal/values/{valueId}/confirm
PUT  /api/portal/values/{valueId}/flag
POST /api/portal/sessions
PUT  /api/portal/sessions/{sessionId}/complete
```

### 3b — React app scaffold
```bash
cd ClientPortal
npm create vite@latest customer-portal -- --template react
```
Add Bootstrap, react-router-dom, @tanstack/react-query, react-hook-form, sass.

### 3c — Pages
- `CustomerLookupPage` — enter email or CustomerCode
- `ValidationFormPage` — dynamic form, confirm/flag per field, grouped by section
- `CompletePage` — thank-you, confirmed/flagged counts

---

## Phase 4 — Authentication 🔲 NOT STARTED

**Goal:** Lock both portals behind Azure AD. Customer portal uses magic links.

**Dependency:** Complete Phases 1–3 first.

### 4a — API
- Apply `[Authorize]` to all admin controllers
- `Microsoft.Identity.Web` is already installed — wire it up

### 4b — Admin SPA
- Re-add `@azure/msal-browser` and `@azure/msal-react`
- Restore Bearer token interceptor in `src/api/client.js`
- Wrap app in `MsalProvider`

### 4c — Customer portal (magic link)
- Token generation service — one-time token + 24hr expiry
- Email sender (SendGrid or SMTP)
- Session table in DB
- Customer enters email → link sent → portal exchanges token for session

### 4d — Azure AD setup

Two app registrations needed:
1. `CustomerValidation-API` — exposes scopes + app roles (`SuperAdmin`, `OrgAdmin`, `Reviewer`)
2. `CustomerValidation-AdminSPA` — public client, SPA redirect URIs

---

## Phase 5 — Customer Detail Page 🔲 NOT STARTED

**Goal:** Drill into a customer from the customers list to see their field values and history.

**Route:** `/organizations/:organizationId/customers/:customerId`

- Read-only view of all field values, grouped by section
- Field label, current value, confirmed/flagged status per field
- Change history table: old value → new value + who + timestamp
- Breadcrumb: Organisations → Org → Customers → Customer name

**API endpoints already exist:**
- `GET /api/customers/{customerId}/values`
- `GET /api/customers/{customerId}/values/history`

---

## Known issues / technical debt

| Item | Priority | Notes |
|---|---|---|
| `DsiplayOrder` typo | Never fix | Established in DB — always use `DsiplayOrder` in SQL |
| No file size limit on import | Low | Reject files over 10 MB in `ImportController` |
| No async import progress | Low | Import is sync; large files need background job + polling |
| Abbreviation not required on create | Low | Warn on org form when navigating to import |
| No customer detail page | Phase 5 | CustomersPage rows have no drill-down yet |

---

## Phase sequence summary

```
Phases 1 + 2 ✓  →  Phase 3  →  Phase 4  →  Phase 5
Admin + Import      Customer     Auth         Customer
complete            portal       (all)        detail
```

Phase 5 (customer detail) is small and can be done any time since the API is already built.
