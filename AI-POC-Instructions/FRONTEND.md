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
      target: 'https://localhost:7017',
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
├── components/
│   ├── common/
│   │   └── index.jsx      ← shared components
│   ├── fields/
│   │   └── FieldOptionsModal.jsx
│   └── layout/
│       └── AppLayout.jsx  ← sidebar + topbar
├── hooks/
│   └── useApi.js          ← React Query hooks
├── pages/
│   ├── ClientsPage.jsx
│   └── FieldDefinitionsPage.jsx
└── styles/
    └── main.scss          ← Bootstrap overrides + layout
```

---

## API Client (`src/api/client.js`)

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
api.get('/clients', { params: { includeInactive: true } })
// fetches: /api/clients?includeInactive=true
```

---

## React Query (`src/hooks/useApi.js`)

All server state goes through React Query hooks. Query keys are
centralised in the `QK` object to keep cache invalidation consistent.

```js
export const QK = {
  clients:  (inactive) => ['clients', inactive],
  client:   (id)       => ['clients', id],
  fields:   (clientId) => ['fields', clientId],
  options:  (fieldId)  => ['options', fieldId],
  values:   (custId)   => ['values', custId],
  history:  (custId)   => ['history', custId],
}
```

**Pattern — reading data:**
```jsx
const { data: clients, isLoading, isError } = useClients()
```

**Pattern — mutations:**
```jsx
const createMutation = useCreateClient()
await createMutation.mutateAsync({ clientName: 'Acme', clientCode: 'ACME' })
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

Routes are defined in `main.jsx` as children of `<AppLayout />`:
```jsx
<Route element={<AppLayout />}>
  <Route index element={<Navigate to="/clients" replace />} />
  <Route path="clients" element={<ClientsPage />} />
  <Route path="clients/:clientId/fields" element={<FieldDefinitionsPage />} />
</Route>
```

---

## Existing Pages

### ClientsPage (`/clients`)
- Table of all clients with name, code, active status, created date
- Create client modal (name + code)
- Edit client modal (name + code + active toggle)
- Deactivate with confirmation dialog
- Show inactive toggle
- Links to field definitions per client

### FieldDefinitionsPage (`/clients/:clientId/fields`)
- Breadcrumb navigation back to clients
- Drag-to-reorder table using `@dnd-kit`
- Field type badges colour-coded by type
- Create field modal — all field properties, type-aware (shows validation
  options for number/text, shows options hint for dropdown/multiselect)
- Edit field modal (fieldKey disabled — cannot change after creation)
- FieldOptionsModal — add/reorder/remove dropdown options with bulk save
- Remove field with confirmation (soft-deactivate, data retained)

---

## Styling (`src/styles/main.scss`)

Bootstrap 5 is imported with overrides. Key custom variables:

```scss
$primary:   #1a56db;
$dark:      #111928;   // sidebar background
$font-family-sans-serif: 'DM Sans', system-ui, sans-serif;
$sidebar-width: 240px;
$topbar-height: 56px;
```

Custom CSS classes:
- `.admin-card` — white card with border and shadow
- `.data-table` — borderless table with hover rows
- `.badge-active` / `.badge-inactive` — status pills
- `.badge-type` — field type pills
- `.drag-handle` — grab cursor for drag rows
- `.breadcrumb-bar` — breadcrumb navigation
- `.empty-state` — centred no-data state

---

## Pages Not Yet Built

| Page | Route | Description |
|---|---|---|
| Customers | `/clients/:clientId/customers` | Customer list with progress bars |
| Customer detail | `/clients/:clientId/customers/:customerId` | Field values + history |
| Import | `/clients/:clientId/import` | CSV/Excel upload + field mapping wizard |
| Import history | `/clients/:clientId/import/history` | Past import batches |
| Dashboard | `/` | Summary stats across all clients |
