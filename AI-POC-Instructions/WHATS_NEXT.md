# What's Next

Everything not yet built, roughly in build order.

---

## Priority 1 — Core Admin Features

### Customers page (Admin SPA)
**Route:** `/clients/:clientId/customers`

Not yet started. The nav item exists in the sidebar but has no page.

Needs:
- Customer list table with search by name or email
- Progress bar per customer (confirmed fields / total fields)
- Flagged field count badge
- Last activity date
- Stat cards at top: Total, Fully confirmed, In progress, Flagged
- Link to customer detail view

**Route:** `/clients/:clientId/customers/:customerId`

- Read-only view of all field values for that customer
- Field label, current value, confirmed/flagged status per field
- Change history table showing old value → new value + timestamp

---

### Dashboard page (Admin SPA)
**Route:** `/`

Currently redirects to `/clients`. Needs a real dashboard showing:
- Total clients, total customers, total fields configured
- Validation progress across all clients
- Recent import activity
- Customers with flagged fields needing attention

---

## Priority 2 — Import Feature

Full CSV/Excel import pipeline. See `IMPORT.md` for complete design.

### Database ✓ DONE
- `ImportBatches` — created
- `ImportColumnMappings` — created
- `ImportValueMappings` — created
- `ImportErrors` — created
- `SavedColumnMappings` — created
- `FileType` column on ImportBatches — created
- `DuplicateStrategy` column on ImportBatches — created

### NuGet packages (not yet installed)
```xml
<PackageReference Include="CsvHelper"  Version="33.0.1" />
<PackageReference Include="ClosedXML"  Version="0.104.2" />
```

### API endpoints (not yet built)
```
POST /api/import/{orgId}/upload
GET  /api/import/{orgId}/mappings?fingerprint={hash}
POST /api/import/{batchId}/mappings
POST /api/import/{batchId}/preview
POST /api/import/{batchId}/execute
GET  /api/import/{orgId}/batches
GET  /api/import/{batchId}/errors
```

### C# classes needed
- `IImportRepository` + `ImportRepository`
- `IImportService` + `ImportService`
- `CsvParser` — reads CSV, returns headers + rows
- `ExcelParser` — reads XLSX/XLS, returns headers + rows
- `CustomerCodeGenerator` — static class, `Generate(abbreviation)`
- `ImportController`
- `ImportBatchDto`, `ImportColumnMappingDto`, `ImportPreviewDto`

### Admin SPA pages (not yet built)
- Import wizard — 5-step stepper (`/clients/:clientId/import`)
  1. File upload (drag/drop or picker)
  2. Auto-match results
  3. Manual mapping for unmatched columns
  4. Preview table
  5. Execution progress + completion summary
- Import history (`/clients/:clientId/import/history`)

---

## Priority 3 — Customer Validation Portal

Separate React app for customers to review and confirm their data.
Location: `/ClientPortal/customer-portal/` (Vite scaffolded, not built)
Port: 5174

### API endpoints (not yet built)
```
GET  /api/portal/customers/{identifier}
PUT  /api/portal/values/{valueId}/confirm
PUT  /api/portal/values/{valueId}/flag
POST /api/portal/sessions
PUT  /api/portal/sessions/{sessionId}/complete
```

### Pages needed
1. `CustomerLookup` — enter email or customer code to find record
2. `ValidationForm` — dynamic form driven by FieldDefinitions
   - Renders correct widget per FieldType (input/date/select/checkbox)
   - Confirm / Flag button per field
   - Progress bar (confirmed / total)
   - Sections grouping fields by FieldSection
3. `Complete` — summary of confirmed vs flagged, thank you message

### Field rendering by type
| FieldType | Widget |
|---|---|
| `text` | `<input type="text">` |
| `number` | `<input type="number">` with min/max |
| `date` | `<input type="date">` |
| `datetime` | `<input type="datetime-local">` |
| `checkbox` | Toggle / checkbox |
| `dropdown` | `<select>` populated from FieldOptions |
| `multiselect` | Checkbox list from FieldOptions |

---

## Priority 4 — Authentication

Both portals currently have no authentication.
Azure AD is configured in the API (`Microsoft.Identity.Web` installed)
but `[Authorize]` attributes are not applied.

### When adding auth:
- Apply `[Authorize]` to all controllers
- Admin SPA: reinstate MSAL (`@azure/msal-browser`, `@azure/msal-react`)
  and restore the Bearer token request interceptor in `src/api/client.js`
- Customer portal: magic link authentication (passwordless)
  - Customer enters email → receives one-time link → gets session token
  - Token expires after 24 hours / single use
  - Needs: token generation service, email sender, session table

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

## Priority 5 — Organisation Management in Admin SPA

The API has full organisation CRUD but the admin SPA has no
Organisation management pages yet.

Needs:
- Organisations list page
- Create/edit organisation modal (including Abbreviation field — required for import)
- Abbreviation validation: max 4 chars, unique
- Warning shown if Abbreviation is missing when navigating to import

---

## Known Issues / Technical Debt

| Item | Notes |
|---|---|
| `DsiplayOrder` typo | Column name has typo in DB. Do not fix — data is established. Always use `DsiplayOrder` in SQL. |
| No pagination on customer list | API returns all customers. Add `page`/`pageSize` params when customer counts grow. |
| No file size limit on import | API should reject files over a configurable max (e.g. 10MB). |
| No async import progress | Import executes synchronously. Large files need a background job + polling endpoint. |
| Abbreviation not required on create | Org can be created without Abbreviation. Import will fail later. Should warn on org form. |

---

## Migration Scripts Run Order

```
1. (base schema)
2. SeedData.sql                              -- 50 test customers
3. SeedDataFieldOptions_States.sql           -- US states
4. SeedDataFieldOptions_HighestDegree.sql    -- degree levels (generate this)
5. Migration_ImportTables.sql               -- import tables
6. Migration_AbbreviationAndImportUpdates.sql
```
