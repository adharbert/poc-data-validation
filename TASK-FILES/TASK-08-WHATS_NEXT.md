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
| Customers | Paginated list, create/edit/activate/deactivate |
| Contracts | Per-org, single active constraint enforced |
| Marketing Projects | Per-org, multiple active allowed |
| Dashboard | Global stat cards, org comparison chart, expiring projects list, org search filter |
| Import (5-step wizard) | Upload CSV/Excel, column mapping, value mapping, preview, execute |
| Import Staging | Resolve unmatched columns post-import |
| Breadcrumb navigation | All sub-pages show Organisations → Org name → Page breadcrumb |
| Consistent date formatting | All dates display as MM/dd/yyyy via shared `fmtDate()` util |
| Serilog sinks | Console always on; DB sinks (InformationLogs/ErrorLogs) toggled by config |
| SVG logo | PCI logo in sidebar; collapses to icon when sidebar is collapsed |
| Unit test suite | `POC.CustomerValidation.Test` — 142 xUnit tests, all 10 controllers, ≥ 90% coverage |

---

## Priority 1 — Customer Detail Page (Admin SPA)

**Route:** `/organizations/:organizationId/customers/:customerId`

Not yet started. Link from CustomersPage row.

Needs:
- Read-only view of all field values for that customer, grouped by section
- Field label, current value, confirmed/flagged status per field
- Change history table: old value → new value + who + timestamp
- Breadcrumb: Organisations → Org name → Customers → Customer name

**API endpoints already exist:**
- `GET /api/customers/{customerId}/values`
- `GET /api/customers/{customerId}/values/history`

---

## Priority 2 — Customer Validation Portal

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

## Priority 3 — Authentication

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
| `DsiplayOrder` typo | Column name has typo in DB. **Do not fix** — data is established. Always use `DsiplayOrder` in SQL. |
| No file size limit on import | API should reject files over a configurable max (e.g. 10 MB). |
| No async import progress | Import executes synchronously. Large files need a background job + polling endpoint. |
| Abbreviation not required on create | Org can be created without Abbreviation. Import will fail later — warn on org form. |
| No customer detail page | CustomersPage rows have no drill-down yet — API already exists (`GET /api/customers/{id}/values`). |

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
