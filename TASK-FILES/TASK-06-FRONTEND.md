# Frontend — Admin SPA

React 18 + Vite application for the admin portal.

Location: `ClientAdmin/datavalidation-portal/`

---

## Stack

| Package | Version | Purpose |
|---|---|---|
| react | 18.x | UI framework |
| vite | 6.x | Build tool + dev server |
| react-router-dom | 6.x | Client-side routing |
| @tanstack/react-query | 5.x | Server state management |
| react-hook-form | 7.x | Form handling |
| bootstrap | 5.3.x | CSS framework |
| sass | 1.x | SCSS preprocessing |
| @dnd-kit/core | 6.x | Drag and drop |
| @dnd-kit/sortable | 8.x | Sortable lists |
| concurrently | dev | Run API + React together |

**Removed / not used:**
- ~~axios~~ — removed, replaced with native `fetch`
- ~~TypeScript~~ — plain JavaScript only
- ~~@azure/msal-browser~~ — removed for POC phase
- ~~@azure/msal-react~~ — removed for POC phase

---

## Running

```bash
# Both API and React together
npm run dev:all

# React only
npm run dev        # http://localhost:5173

# API only (from this folder)
npm run dev:api
```

### package.json scripts
```json
{
  "dev":     "vite",
  "dev:api": "dotnet run --project ../../POC.CustomerValidation/POC.CustomerValidation.API",
  "dev:all": "concurrently --names \"API,CLIENT\" --prefix-colors \"cyan,magenta\" \"npm run dev:api\" \"npm run dev\"",
  "build":   "vite build",
  "preview": "vite preview"
}
```

---

## Vite Configuration

`vite.config.js` sets up the `@` path alias and proxies `/api` calls
to the .NET API so React code never contains the API port number.

```js
server: {
  port: 5173,
  proxy: {
    '/api': {
      target: 'https://localhost:7124',
      changeOrigin: true,
      secure: false       // self-signed dev cert
    }
  }
}
```

All API calls use relative URLs: `/api/organisations`, `/api/fields/...`

---

## File Structure

```
src/
├── api/
│   ├── client.js          ← fetch wrapper (see below)
│   └── services.js        ← all API call functions
├── assets/
│   ├── scss/
│   │   └── main.scss      ← Bootstrap overrides + all custom styles (source)
│   └── css/
│       ├── main.css        ← compiled by Live SASS Compiler (do not edit)
│       └── main.min.css    ← minified output (do not edit)
├── components/
│   ├── common/
│   │   └── index.jsx      ← shared components
│   └── layout/
│       └── AppLayout.jsx  ← sidebar + topbar
├── hooks/
│   └── useApi.js          ← React Query hooks
├── pages/
│   ├── DashboardPage.jsx
│   ├── OrganizationsPage.jsx
│   ├── OrgDetailPage.jsx  ← org landing: stats, contracts, projects, nav tiles
│   ├── CustomersPage.jsx
│   ├── InputsPage.jsx
│   ├── ImportPage.jsx
│   └── ImportStagingPage.jsx
└── utils/
    └── dates.js           ← fmtDate(str) → MM/dd/yyyy  |  fmtPhone(str) → XXX.XXX.XXXX
```

---

## API organization (`src/api/client.js`)

Thin `fetch` wrapper that mirrors the axios call signature so
`services.js` can use `.then(r => r.data)` on every call.

```js
// Returns { data } on success, rejects with ApiError on failure
const api = {
  get:    (path, options)       => request('GET',    path, options),
  post:   (path, body, options) => request('POST',   path, { ...options, body }),
  put:    (path, body, options) => request('PUT',    path, { ...options, body }),
  patch:  (path, body, options) => request('PATCH',  path, { ...options, body }),
  delete: (path, options)       => request('DELETE', path, options),
}
```

Query string params are passed as `{ params: { key: value } }`:
```js
api.get('/organizations', { params: { includeInactive: true } })
// fetches: /api/organizations?includeInactive=true
```

---

## React Query (`src/hooks/useApi.js`)

All server state goes through React Query hooks. Query keys are
centralised in the `QK` object to keep cache invalidation consistent.

```js
export const QK = {
  organizations:    (inactive)              => ['organizations', inactive],
  organization:     (id)                    => ['organizations', id],
  fields:           (orgId, inactive)       => ['fields', orgId, inactive],
  fieldOptions:     (fieldId)               => ['fieldOptions', fieldId],
  customers:        (orgId, page)           => ['customers', orgId, page],
  contracts:        (orgId)                 => ['contracts', orgId],
  projects:         (orgId)                 => ['projects', orgId],
  sections:         (orgId)                 => ['sections', orgId],
  section:          (orgId, sectionId)      => ['sections', orgId, sectionId],
  formPreview:      (orgId, customerId)     => ['formPreview', orgId, customerId],
  dashboardStats:   ()                      => ['dashboard', 'stats'],
  expiringProjects: ()                      => ['dashboard', 'expiring'],
  importBatches:    (orgId)                 => ['importBatches', orgId],
  importBatch:      (orgId, batchId)        => ['importBatch', orgId, batchId],
  savedMappings:    (orgId, fingerprint)    => ['savedMappings', orgId, fingerprint],
  staging:          (orgId, status)         => ['staging', orgId, status],
}
```

**Pattern — reading data:**
```jsx
const { data: organizations, isLoading, isError } = useOrganizations()
```

**Pattern — mutations:**
```jsx
const createMutation = useCreateOrganization()
await createMutation.mutateAsync({ organizationName: 'Acme', organizationCode: 'ACME' })
```

---

## Common Components (`src/components/common/index.jsx`)

| Component | Props | Purpose |
|---|---|---|
| `Spinner` | `size?: 'sm'/'md'/'lg'` | Loading indicator |
| `StatusBadge` | `active: bool` | Green/grey pill |
| `FieldTypeBadge` | `type: FieldType` | Coloured type pill |
| `EmptyState` | `title, description?, action?` | No-data placeholder |
| `PageHeader` | `title, subtitle?, actions?` | Page title row |
| `LoadingState` | `message?` | Full-area spinner |
| `ErrorAlert` | `message: string` | Red alert bar |
| `ConfirmModal` | `show, title, message, onConfirm, onCancel, danger?, loading?` | Confirmation dialog |
| `ToastProvider` | wraps app | Toast context provider |
| `useToast` | hook | `showToast(message, variant?)` |

---

## Layout

`AppLayout.jsx` provides the two-column shell:
- Dark sidebar (240px) with nav links, collapsible to 56px
- White topbar (56px) with page title
- Grey page body with overflow-y scroll

Routes are defined in `App.jsx` as children of `<AppLayout />`:
```jsx
<Route element={<AppLayout />}>
  <Route index element={<Navigate to="/dashboard" replace />} />
  <Route path="dashboard"                                          element={<DashboardPage />} />
  <Route path="organizations"                                      element={<OrganizationsPage />} />
  <Route path="organizations/:organizationId"                      element={<OrgDetailPage />} />
  <Route path="organizations/:organizationId/customers"            element={<CustomersPage />} />
  <Route path="organizations/:organizationId/inputs"               element={<InputsPage />} />
  <Route path="organizations/:organizationId/import"               element={<ImportPage />} />
  <Route path="organizations/:organizationId/import-staging"       element={<ImportStagingPage />} />
</Route>
```

The sidebar shows per-org sub-navigation (Overview, Customers, Inputs, Import, Staging) when any
`/organizations/:organizationId/...` route is active. The `orgId` is read from `useParams`
in `AppLayout` to decide whether to render the sub-nav. The Overview link uses `end` prop so
it only matches the exact org detail route, not all sub-routes.

Sub-pages (Customers, Inputs, Import, Staging) all show a 3-level breadcrumb:
**Organisations → [Org Name] → [Current Page]** — the org name link navigates to `OrgDetailPage`.
Each sub-page calls `useOrganization(organizationId)` to get the org name for the breadcrumb.

---

## Existing Pages

### DashboardPage (`/dashboard`)
- Global stat cards: Active Organisations, Expiring Projects count, Total Customers, Overall Verified %
- Organisation comparison table: name, customer count, verification progress bar, active projects count — org name links to OrgDetailPage
- Customer distribution bar chart (shown when > 1 org): stacked bars show total vs verified per org
- Expiring projects list (projects within configured warning window of `MarketingEndDate`)

### OrgDetailPage (`/organizations/:organizationId`)
- Per-org stat cards: Total Customers, Verified (with % complete), Active Projects, Status
- Validation progress bar across all customers for this org
- Contracts section: timeline list with start/end dates, urgency colouring (red ≤7d, amber ≤30d, green)
- Marketing Projects section: timeline list with progress bars showing elapsed time
- Navigation tiles linking to Customers, Inputs, Import, Staging sub-pages
- Phone number displayed as `XXX.XXX.XXXX` via `fmtPhone()`

### OrganizationsPage (`/organizations`)
- Table of all organisations with name, code, active status, created date
- Create organisation modal (name + code)
- Edit organisation modal (name + code + active toggle)
- Deactivate with confirmation dialog
- Show inactive toggle
- Links into per-org sub-pages (Customers, Inputs, Import)

### CustomersPage (`/organizations/:organizationId/customers`)
- Paginated table of customers for the org
- Create / edit customer modals
- Activate / deactivate with confirmation (data retained)
- Show inactive toggle

### InputsPage (`/organizations/:organizationId/inputs`)
- **Sections** and **fields** managed on a single page (replaces the old FieldDefinitionsPage)
- Section cards displayed in drag-and-drop order (outer `DndContext`)
- Fields within each section also drag-to-reorder (inner `DndContext` per section)
- "Unassigned" group at bottom for fields with no section
- **New Section modal** — name, display order, plus checkbox list to assign unassigned fields on creation
- **Edit Section modal** — name and display order; activate/deactivate button on the card
- **New/Edit Input (field) modal** — all field properties; includes phone display format dropdown when `fieldType = 'phone'`
- **Field Options modal** — manage dropdown/multiselect option values with bulk save
- Activate/deactivate for both sections and fields (soft-delete, data retained for reporting)
- **Form Preview panel** — collapsible, select any customer to see a read-only rendered form
  with their current saved values, grouped by section; phone fields formatted per `displayFormat`

### ImportPage (`/organizations/:organizationId/import`)
- 5-step import wizard: Upload → Column Mapping → Value Mapping → Preview → Execute
- File upload (CSV, XLSX, XLS)
- Auto-match columns to field keys; manual override per column
- Saved mapping reuse (matched by file fingerprint)
- Preview table: first 10 rows with per-row validation status
- Execution with result summary

### ImportStagingPage (`/organizations/:organizationId/import-staging`)
- Lists staged (unresolved) columns from imports that couldn't be auto-matched
- Resolve: map to an existing field, create a new field, or dismiss
- Delete individual staging records

---

## Styling (`src/assets/scss/main.scss`)

SCSS source lives in `src/assets/scss/`. The VS Code **Live SASS Compiler** extension
(Glenn2223 fork) watches for changes and compiles to `src/assets/css/main.css` and
`main.min.css`. Vite imports the compiled CSS — it does **not** process the SCSS itself.

Bootstrap 5 is imported via a relative path (required for the standalone SASS compiler):
```scss
@import '../../../node_modules/bootstrap/scss/bootstrap';
```

Key custom variables:
```scss
$primary:   #1a56db;
$dark:      #111928;   // sidebar background
$font-family-sans-serif: 'DM Sans', system-ui, sans-serif;
```

Custom CSS classes:
- `.admin-card` — white card with border and shadow
- `.stat-card` — dashboard stat card with icon slot
- `.data-table` — borderless table with hover rows
- `.badge-active` / `.badge-inactive` — status pills
- `.badge-type` — field type pills (colour per type); includes `.badge-phone`
- `.badge-status` — import batch status pills
- `.drag-handle` — grab cursor for sortable rows
- `.breadcrumb-bar` — breadcrumb navigation (Organisations → Org → Page)
- `.empty-state` — centred no-data state
- `.page-header` — title + actions row
- `.wizard-steps` — import wizard step indicator
- `.btn-xs` — extra-small button (Bootstrap has `sm` but not `xs`)
- `.section-card` / `.section-card-header` / `.section-card-name` — Inputs page section cards
- `.section-inactive` / `.section-unassigned` — section card modifiers
- `.field-row` / `.field-row-key` / `.field-row-label` — field rows within sections
- `.section-empty-hint` — placeholder text for empty sections
- `.section-field-pick-list` / `.section-field-pick-item` — checkbox list in Section modal
- `.preview-form-wrap` / `.preview-section` / `.preview-section-title` — Form Preview panel
- `.org-stat-card` / `.org-stat-icon` / `.org-stat-body` / `.org-stat-label` / `.org-stat-value` / `.org-stat-sub` — OrgDetailPage stat cards
- `.org-section-title` — section heading inside OrgDetailPage cards
- `.org-timeline-list` / `.org-timeline-item` — contracts + projects timeline rows
- `.org-nav-tiles` / `.org-nav-tile` / `.org-nav-tile-icon` / `.org-nav-tile-label` / `.org-nav-tile-desc` — navigation tile grid

---

## Pages Not Yet Built

| Page | Route | Notes |
|---|---|---|
| Customer detail | `/organizations/:organizationId/customers/:customerId` | Field values + change history for one customer |
| Customer Validation Portal | separate Vite app on port 5174 | `ClientPortal/customer-portal/` — not yet scaffolded |
