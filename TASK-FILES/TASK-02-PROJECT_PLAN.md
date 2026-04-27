# Project Plan

Phased build plan derived from `WHATS_NEXT.md`. Each phase has a clear goal,
the tasks required, and notes on dependencies between phases.

---

## Phase 1 — Core Admin Features

**Goal:** Complete the admin SPA so staff can view customers and their validation state.

### 1a — Customers list page

Route: `/clients/:clientId/customers`

**API** (check which endpoints exist first — some may already be built)
- [ ] `GET /api/customers?orgId={id}` — list customers for an org with search params
- [ ] Response must include: `confirmedCount`, `totalFields`, `flaggedCount`, `lastActivityAt`

**Admin SPA**
- [ ] `CustomersPage.jsx` — table with search by name/email
- [ ] Progress bar per row (`confirmedFields / totalFields`)
- [ ] Flagged count badge per row
- [ ] Stat cards at top: Total / Fully confirmed / In progress / Flagged
- [ ] Link to customer detail view per row
- [ ] Wire route in `main.jsx`: `clients/:clientId/customers`
- [ ] Add sidebar nav link

### 1b — Customer detail page

Route: `/clients/:clientId/customers/:customerId`

**API** — already exists:
- `GET /api/customers/{customerId}/values`
- `GET /api/customers/{customerId}/values/history`

**Admin SPA**
- [ ] `CustomerDetailPage.jsx` — read-only field value list
- [ ] Field label, current value, confirmed/flagged status per row
- [ ] Change history table: old value → new value + who + timestamp
- [ ] Breadcrumb: Clients → Client name → Customers → Customer name
- [ ] Wire route in `main.jsx`: `clients/:clientId/customers/:customerId`

### 1c — Dashboard page

Route: `/` (currently redirects to `/clients`)

**Admin SPA**
- [ ] `DashboardPage.jsx`
- [ ] Stat cards: total clients, total customers, total fields configured
- [ ] Validation progress across all clients (aggregate confirmed %)
- [ ] Customers with flagged fields (top N list)
- [ ] Recent import activity (once import is built — placeholder for now)
- [ ] Replace the redirect in `main.jsx`

---

## Phase 2 — Import Feature

**Goal:** Allow ETL teams to upload CSV/Excel files and map columns to field definitions.

**Design reference:** `IMPORT.md` for full flow, value mapping rules, and saved mapping reuse.

### 2a — NuGet packages

- [ ] Add `CsvHelper 33.0.1` to `.csproj`
- [ ] Add `ClosedXML 0.104.2` to `.csproj`

### 2b — Database (already done)

The import tables are already created:
- `ImportBatches` ✓
- `ImportColumnMappings` ✓
- `ImportValueMappings` ✓
- `ImportErrors` ✓
- `SavedColumnMappings` ✓

### 2c — API layer

- [ ] `CustomerCodeGenerator` static class (`Generate(abbreviation)` — Abbreviation + ULID[..10])
- [ ] `CsvParser` — reads CSV, returns `(string[] headers, string[][] rows)`
- [ ] `ExcelParser` — reads XLSX/XLS via ClosedXML, same return shape
- [ ] `IImportRepository` + `ImportRepository` (Dapper)
- [ ] `IImportService` + `ImportService`
- [ ] `ImportController` with routes:
  - `POST /api/import/{orgId}/upload`
  - `GET  /api/import/{orgId}/mappings?fingerprint={hash}`
  - `POST /api/import/{batchId}/mappings`
  - `POST /api/import/{batchId}/preview`
  - `POST /api/import/{batchId}/execute`
  - `GET  /api/import/{orgId}/batches`
  - `GET  /api/import/{batchId}/errors`
- [ ] DTOs: `ImportBatchDto`, `ImportColumnMappingDto`, `ImportPreviewDto`
- [ ] Register `IImportRepository` (Scoped) and `IImportService` (Scoped) in DI before `builder.Build()`

### 2d — Admin SPA

- [ ] `ImportWizardPage.jsx` — 5-step stepper at `/clients/:clientId/import`
  1. File upload (drag/drop or picker)
  2. Auto-match results (green / amber / grey per column)
  3. Manual mapping for unmatched columns + optional value translation
  4. Preview table (first 10 rows, required-field violations highlighted)
  5. Execution progress + completion summary
- [ ] `ImportHistoryPage.jsx` at `/clients/:clientId/import/history`
  - Table of batches: file name, date, status, row counts, errors link
  - Re-import button (reuse saved mapping)
- [ ] Add import nav item to sidebar (under each client)
- [ ] Wire routes in `main.jsx`

### 2e — Org Abbreviation guard

Before import can run, `Org.Abbreviation` must be set (used for `CustomerCode`).
- [ ] Warn admin on the import page if Abbreviation is missing
- [ ] Surface Abbreviation field on the Create/Edit org modal (Phase 5 covers the full org page)

---

## Phase 3 — Customer Validation Portal

**Goal:** Separate React app for customers to review and confirm their field values.

**Location:** `ClientPortal/customer-portal/` (scaffold with Vite — not yet created)
**Port:** 5174

### 3a — API endpoints

- [ ] `GET  /api/portal/customers/{identifier}` — lookup by email or CustomerCode, return customer + all field values
- [ ] `PUT  /api/portal/values/{valueId}/confirm` — mark field as confirmed
- [ ] `PUT  /api/portal/values/{valueId}/flag` — flag field with a note
- [ ] `POST /api/portal/sessions` — create a validation session record
- [ ] `PUT  /api/portal/sessions/{sessionId}/complete` — complete the session

These are **new controllers** — create a `Portal` area or prefix routes with `/api/portal/`.

### 3b — React app scaffold

- [ ] `npm create vite@latest customer-portal -- --template react` in `ClientPortal/`
- [ ] Add Bootstrap 5, react-router-dom, `@tanstack/react-query`, `react-hook-form`
- [ ] Set up Vite proxy for `/api` → `https://localhost:7017`
- [ ] Copy `src/api/client.js` fetch wrapper from admin SPA (same pattern)

### 3c — Pages

- [ ] `CustomerLookupPage.jsx` — enter email or CustomerCode, POST to `/api/portal/sessions`
- [ ] `ValidationFormPage.jsx` — dynamic form driven by FieldDefinitions:
  - Render correct widget per FieldType (see mapping table in `WHATS_NEXT.md`)
  - Confirm / Flag button per field
  - Progress bar: confirmed / total
  - Group fields by FieldSection if present
- [ ] `CompletePage.jsx` — thank-you, confirmed count, flagged count, next steps

### 3d — package.json + dev:all

- [ ] Add portal dev script to admin SPA `package.json` or root-level script
- [ ] Portal dev server runs on port 5174

---

## Phase 4 — Authentication

**Goal:** Lock both portals behind Azure AD. Customer portal uses magic links.

**Dependency:** Complete Phases 1–3 first. Auth is added on top.

### 4a — API

- [ ] Apply `[Authorize]` to all admin controllers
- [ ] Apply separate `[Authorize]` policy to portal controllers (or leave open if magic-link session is the auth)
- [ ] `Microsoft.Identity.Web` is already installed — just wire it up

### 4b — Admin SPA

- [ ] Re-add `@azure/msal-browser` and `@azure/msal-react`
- [ ] Restore Bearer token request interceptor in `src/api/client.js`
- [ ] Wrap app in `MsalProvider`

### 4c — Customer portal (magic link)

- [ ] Token generation service — one-time token + 24hr expiry
- [ ] Email sender (SendGrid or SMTP)
- [ ] Session table in DB
- [ ] Customer enters email → API sends link → link carries token → portal exchanges for session

### 4d — Azure AD setup

See `docs/AZURE_AD_SETUP.md` for full registration steps.

Two app registrations needed:
1. `CustomerValidation-API` — exposes scopes + app roles (`SuperAdmin`, `OrgAdmin`, `Reviewer`)
2. `CustomerValidation-AdminSPA` — public client, SPA redirect URIs

---

## Phase 5 — Organisation Management in Admin SPA

**Goal:** Build out the missing admin UI for creating and editing organisations.

Currently: API has full org CRUD; admin SPA has no org management pages.

### 5a — Organisations list page

- [ ] `OrganisationsPage.jsx` at `/organisations`
- [ ] Table: name, code, abbreviation, active status, created date
- [ ] Create org modal (name, code, abbreviation — required for import, max 4 chars)
- [ ] Edit org modal (same fields + active toggle)
- [ ] Deactivate with confirmation
- [ ] Abbreviation validation: max 4 chars, unique

### 5b — Abbreviation warning

- [ ] If Abbreviation is missing, show warning banner when navigating to the import page

### 5c — Routing

- [ ] Add `/organisations` route to `main.jsx`
- [ ] Add Organisations nav item in sidebar

---

## Known issues / technical debt

| Item | Priority | Notes |
|---|---|---|
| `DsiplayOrder` typo | Never fix | Established in DB — always use `DsiplayOrder` in SQL |
| No pagination on customer list | Phase 1 | Add `page`/`pageSize` when building Phase 1a |
| No file size limit on import | Phase 2 | Reject files over 10MB in `ImportController` |
| No async import progress | Phase 2+ | Import is sync; large files need background job + polling |
| Abbreviation not required on create | Phase 2 | Warn on org form when navigating to import |

---

## Phase sequence summary

```
Phase 1  →  Phase 2  →  Phase 3  →  Phase 4  →  Phase 5
Admin        Import       Customer     Auth         Org mgmt
customers    pipeline     portal       (all)        UI
```

Phase 5 (org management) can be built in parallel with Phase 2 or 3
since it is frontend-only work against an already-complete API.
