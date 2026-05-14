# ETL Import Guide — API-Driven Setup

This document is for ETL teams configuring imports programmatically via the REST API,
bypassing the UI wizard. The ETL import scope is:

- **Customer core fields** — identity, contact info, dates
- **Key/value fields** — org-specific field definitions stored in `FieldValues`

Address, email detail, and phone detail tables are not part of the ETL import path.

---

## Prerequisites

Before writing any API calls, collect the following from the admin portal or database:

| Item | How to get it |
|---|---|
| `organisationId` | URL of the org detail page, or `GET /api/organisations` |
| `fieldDefinitionId` for each key/value field | `GET /api/organisations/{orgId}/fields` |
| Base URL | `https://localhost:7124` (dev) or your deployed host |

### Getting Field Definition IDs

```http
GET /api/organisations/{orgId}/fields
```

Response includes an array of field definitions. Note the `fieldDefinitionId` and `fieldLabel`
for every field you intend to populate:

```json
{
  "items": [
    { "fieldDefinitionId": "8a1f4c00-...", "fieldLabel": "Gender",         "fieldType": "dropdown",  "fieldKey": "gender" },
    { "fieldDefinitionId": "9b2e5d11-...", "fieldLabel": "Highest Degree", "fieldType": "dropdown",  "fieldKey": "highest_degree" },
    { "fieldDefinitionId": "ac3f6e22-...", "fieldLabel": "Graduation Year", "fieldType": "number",   "fieldKey": "graduation_year" },
    { "fieldDefinitionId": "bd4g7f33-...", "fieldLabel": "Notes",           "fieldType": "text",     "fieldKey": "notes" }
  ]
}
```

---

## Full API Workflow

```
1. Upload file                 →  POST   /api/organisations/{orgId}/imports
2. Save column mappings        →  POST   /api/organisations/{orgId}/imports/{batchId}/mappings
3. Pre-load value aliases      →  POST   /api/organisations/{orgId}/field-option-aliases/bulk
4. Check unresolved values     →  GET    /api/organisations/{orgId}/imports/{batchId}/value-mapping
5. Preview first 10 rows       →  POST   /api/organisations/{orgId}/imports/{batchId}/preview
6. Execute full import         →  POST   /api/organisations/{orgId}/imports/{batchId}/execute
7. Poll for completion         →  GET    /api/organisations/{orgId}/imports/{batchId}
8. Review errors (if any)      →  GET    /api/organisations/{orgId}/imports/{batchId}/errors
```

Steps 3 and 4 can be done in either order. If all aliases are pre-loaded before step 4,
the value-mapping check will return `hasUnresolved: false`.

---

## Step 1 — Upload the File

```http
POST /api/organisations/{orgId}/imports
Content-Type: multipart/form-data

file              = <binary CSV or Excel file>
uploadedBy        = "ETL-Pipeline"
duplicateStrategy = "skip"   # skip | update | error
```

`duplicateStrategy` controls what happens when a customer already exists (matched by email
or `OriginalId`):
- `skip` — leave the existing record unchanged, count as skipped
- `update` — overwrite core customer fields with new values
- `error` — record as an error row, skip the insert

**Response (201 Created):**
```json
{
  "batchId": "3fa85f64-...",
  "fileName": "customers_2026_Q1.csv",
  "headerFingerprint": "abc123",
  "columnMatches": [
    {
      "columnIndex": 0,
      "csvHeader": "first_name",
      "matchStatus": "matched",
      "destinationTable": "customer",
      "destinationField": "FirstName",
      "transformType": "direct",
      "isAutoMatched": true
    },
    {
      "columnIndex": 4,
      "csvHeader": "sex",
      "matchStatus": "unmatched",
      "destinationTable": null,
      "destinationField": null,
      "isAutoMatched": false
    }
  ]
}
```

`matchStatus` values:
- `matched` — system recognised the header and pre-filled the mapping
- `unmatched` — must be mapped manually in step 2

Save the `batchId` — all subsequent calls use it.

---

## Step 2 — Save Column Mappings

Every column in the file must be mapped or explicitly skipped. Unrecognised columns that
are not relevant should use `"destinationTable": "skip"`.

```http
POST /api/organisations/{orgId}/imports/{batchId}/mappings
Content-Type: application/json
```

### Request Schema

```json
{
  "mappings": [
    {
      "csvHeader":        "string",
      "csvColumnIndex":   0,
      "destinationTable": "customer",
      "destinationField": "FirstName",
      "fieldDefinitionId": null,
      "transformType":    "direct",
      "isAutoMatched":    false,
      "saveForReuse":     true,
      "displayOrder":     0,
      "outputs":          []
    }
  ]
}
```

### `destinationTable` — ETL scope

| Value | Writes to |
|---|---|
| `customer` | Core columns on the `Customers` table |
| `field_value` | `FieldValues` key/value store (org-specific fields) |
| `skip` | Column is ignored — not imported |

### `destinationField` — Customer columns

Used when `destinationTable = "customer"`:

| `destinationField` | Type | Notes |
|---|---|---|
| `FirstName` | text | **Required** |
| `LastName` | text | **Required** |
| `OriginalId` | text | **Required** — the client's own ID for this customer |
| `MiddleName` | text | Optional |
| `MaidenName` | text | Optional |
| `DateOfBirth` | date | Optional — `YYYY-MM-DD` or common date formats |
| `Email` | text | Optional — used for deduplication |
| `Phone` | text | Optional |

`CustomerCode` must **not** be mapped — it is auto-generated by the system on insert.

### `fieldDefinitionId`

Required when `destinationTable = "field_value"`. Use the GUID from
`GET /api/organisations/{orgId}/fields`. Set `destinationField` to `null`.

### `transformType` values

| Value | Behaviour |
|---|---|
| `direct` | Raw CSV value stored as-is (use for most columns) |
| `split_full_name` | Parses a combined name column into First/Middle/Last tokens |
| `strip_credentials` | Strips professional suffixes (M.D., Ph.D., etc.) before storing |

`split_full_address` is not used in the ETL path — address data is not imported.

### `split_full_name` — outputs array

When a single column contains a full name, use `transformType: "split_full_name"` and
define where each parsed token goes via the `outputs` array:

```json
{
  "csvHeader": "full_name",
  "csvColumnIndex": 1,
  "destinationTable": "customer",
  "destinationField": null,
  "fieldDefinitionId": null,
  "transformType": "split_full_name",
  "isAutoMatched": false,
  "saveForReuse": true,
  "displayOrder": 1,
  "outputs": [
    { "outputToken": "FirstName",   "destinationTable": "customer", "destinationField": "FirstName",  "sortOrder": 1 },
    { "outputToken": "MiddleName",  "destinationTable": "customer", "destinationField": "MiddleName", "sortOrder": 2 },
    { "outputToken": "LastName",    "destinationTable": "customer", "destinationField": "LastName",   "sortOrder": 3 },
    { "outputToken": "Suffix",      "destinationTable": "skip",     "destinationField": null,         "sortOrder": 4 },
    { "outputToken": "Credentials", "destinationTable": "skip",     "destinationField": null,         "sortOrder": 5 }
  ]
}
```

The parser expects `"LastName, FirstName MiddleName"` format. Tokens not needed can be
set to `"destinationTable": "skip"`.

### `saveForReuse`

Set `true` on every mapping. When the import completes, these mappings are saved under
the file's header fingerprint. The next upload with the same column structure will
auto-load all mappings — the ETL team only needs to define them once per file layout.

---

## Step 3 — Pre-load Value Aliases

For `dropdown` and `multiselect` fields, if the client's file uses different values than
the canonical option keys defined in the system, register aliases before executing.
Aliases are persistent — saved once, applied to all future imports for this organisation.

```http
POST /api/organisations/{orgId}/field-option-aliases/bulk
Content-Type: application/json

{
  "aliases": [
    { "fieldDefinitionId": "8a1f4c00-...", "aliasValue": "M",      "canonicalValue": "male" },
    { "fieldDefinitionId": "8a1f4c00-...", "aliasValue": "F",      "canonicalValue": "female" },
    { "fieldDefinitionId": "8a1f4c00-...", "aliasValue": "Male",   "canonicalValue": "male" },
    { "fieldDefinitionId": "8a1f4c00-...", "aliasValue": "Female", "canonicalValue": "female" },
    { "fieldDefinitionId": "9b2e5d11-...", "aliasValue": "BA",     "canonicalValue": "bachelors" },
    { "fieldDefinitionId": "9b2e5d11-...", "aliasValue": "MS",     "canonicalValue": "masters" },
    { "fieldDefinitionId": "9b2e5d11-...", "aliasValue": "PHD",    "canonicalValue": "doctorate" }
  ]
}
```

Rules:
- `aliasValue` matching is **case-insensitive** at import time
- `canonicalValue` should match the `OptionKey` of a valid option on that field
- Aliases for `text`, `number`, `date` fields are ignored — only `dropdown` and `multiselect` are resolved
- Sending a duplicate `aliasValue` for the same field updates the existing mapping

To view all saved aliases: `GET /api/organisations/{orgId}/field-option-aliases`
To remove an alias: `DELETE /api/organisations/{orgId}/field-option-aliases/{aliasId}`

---

## Step 4 — Check Unresolved Values

After saving aliases, verify that no values in the actual file are still unmatched:

```http
GET /api/organisations/{orgId}/imports/{batchId}/value-mapping
```

**Response:**
```json
{
  "hasUnresolved": false,
  "columns": [
    {
      "csvHeader": "sex",
      "fieldLabel": "Gender",
      "fieldType": "dropdown",
      "knownOptions": ["male", "female", "non-binary"],
      "existingAliases": [
        { "aliasValue": "M", "canonicalValue": "male" },
        { "aliasValue": "F", "canonicalValue": "female" }
      ],
      "unresolvedValues": []
    }
  ]
}
```

If `hasUnresolved: true`, `unresolvedValues` lists every distinct value in the file that
will be stored as-is (not mapped to a canonical option). Add aliases for them if needed,
then re-check.

Unresolved values are **not** import errors — the row still imports, the raw value is just
stored without alias translation. Whether that is acceptable depends on downstream reporting.

---

## Step 5 — Preview (First 10 Rows)

Validates the first 10 rows with all mappings and aliases applied:

```http
POST /api/organisations/{orgId}/imports/{batchId}/preview
```

**Response:**
```json
{
  "headers": ["first_name", "last_name", "client_id", "sex", "highest_degree"],
  "rows": [
    { "rowNumber": 1, "status": "ok",    "values": ["Jane", "Doe",   "LSU-001", "F",  "MS"],  "message": null },
    { "rowNumber": 2, "status": "error", "values": ["",     "Smith", "LSU-002", "M",  "BA"],  "message": "FirstName is required." }
  ],
  "okCount": 9,
  "warningCount": 0,
  "errorCount": 1
}
```

Row `status` values:
- `ok` — row will import cleanly
- `warning` — row will import with a non-fatal issue
- `error` — row will be skipped; reason in `message`

Fix errors before proceeding if the error count is unacceptable.

---

## Step 6 — Execute

```http
POST /api/organisations/{orgId}/imports/{batchId}/execute
```

Returns `202 Accepted` immediately. The import runs server-side in the background.

---

## Step 7 — Poll for Completion

```http
GET /api/organisations/{orgId}/imports/{batchId}
```

Poll every 5 seconds until `status` is `completed` or `failed`:

```json
{
  "batchId": "3fa85f64-...",
  "status": "completed",
  "totalRows": 1500,
  "importedRows": 1487,
  "skippedRows": 8,
  "errorRows": 5,
  "completedAt": "2026-05-13T14:22:11Z"
}
```

Batch status lifecycle:
```
pending → preview → importing → completed
                  ↘ failed
```

If `status = "failed"`, the entire batch failed (not individual rows). Call execute again
to retry — individual row failures are counted in `errorRows`, not a batch failure.

---

## Step 8 — Review Errors

```http
GET /api/organisations/{orgId}/imports/{batchId}/errors
```

```json
[
  {
    "rowNumber": 2,
    "errorType": "system",
    "errorMessage": "FirstName is required.",
    "rawData": "[\"\",\"Smith\",\"LSU-002\",\"M\",\"BA\"]"
  },
  {
    "rowNumber": 47,
    "errorType": "duplicate",
    "errorMessage": "Customer with email 'jane@example.com' already exists.",
    "rawData": "[\"Jane\",\"Doe\",\"LSU-003\",\"F\",\"PHD\"]"
  }
]
```

`errorType` values:
- `system` — validation failure (missing required field, type mismatch, etc.)
- `duplicate` — customer already exists and `duplicateStrategy = "error"`

Log these rows and investigate before re-importing corrections.

---

## Required Fields

Rows missing any of the following are flagged as `error` in preview and skipped during execute:

| CSV column must map to | `destinationTable` | `destinationField` |
|---|---|---|
| First name | `customer` | `FirstName` |
| Last name | `customer` | `LastName` |
| Client's own ID | `customer` | `OriginalId` |

All other fields are optional.

---

## Complete Example — Mapping Payload

Scenario: 6-column file — `fname`, `lname`, `client_id`, `dob`, `sex` (dropdown), `degree` (dropdown).

```json
POST /api/organisations/{orgId}/imports/{batchId}/mappings

{
  "mappings": [
    {
      "csvHeader": "fname",       "csvColumnIndex": 0,
      "destinationTable": "customer", "destinationField": "FirstName",
      "fieldDefinitionId": null,  "transformType": "direct",
      "isAutoMatched": false,     "saveForReuse": true, "displayOrder": 0, "outputs": []
    },
    {
      "csvHeader": "lname",       "csvColumnIndex": 1,
      "destinationTable": "customer", "destinationField": "LastName",
      "fieldDefinitionId": null,  "transformType": "direct",
      "isAutoMatched": false,     "saveForReuse": true, "displayOrder": 1, "outputs": []
    },
    {
      "csvHeader": "client_id",   "csvColumnIndex": 2,
      "destinationTable": "customer", "destinationField": "OriginalId",
      "fieldDefinitionId": null,  "transformType": "direct",
      "isAutoMatched": false,     "saveForReuse": true, "displayOrder": 2, "outputs": []
    },
    {
      "csvHeader": "dob",         "csvColumnIndex": 3,
      "destinationTable": "customer", "destinationField": "DateOfBirth",
      "fieldDefinitionId": null,  "transformType": "direct",
      "isAutoMatched": false,     "saveForReuse": true, "displayOrder": 3, "outputs": []
    },
    {
      "csvHeader": "sex",         "csvColumnIndex": 4,
      "destinationTable": "field_value", "destinationField": null,
      "fieldDefinitionId": "8a1f4c00-0000-0000-0000-000000000001",
      "transformType": "direct",  "isAutoMatched": false,
      "saveForReuse": true,       "displayOrder": 4, "outputs": []
    },
    {
      "csvHeader": "degree",      "csvColumnIndex": 5,
      "destinationTable": "field_value", "destinationField": null,
      "fieldDefinitionId": "9b2e5d11-0000-0000-0000-000000000002",
      "transformType": "direct",  "isAutoMatched": false,
      "saveForReuse": true,       "displayOrder": 5, "outputs": []
    }
  ]
}
```

---

## Resuming a Batch

If the pipeline fails after upload but before execute, resume without re-uploading:

```http
POST /api/organisations/{orgId}/imports/{batchId}/resume
```

Returns the same shape as the upload response. Re-POST mappings, then continue from step 3.
Cannot resume a `completed` batch — start a new import.

---

## Cancelling / Deleting a Batch

```http
POST   /api/organisations/{orgId}/imports/{batchId}/cancel
DELETE /api/organisations/{orgId}/imports/{batchId}
```

Both work on batches in any status.

---

## Tips

1. **Load aliases once, not per import.** Call `POST /field-option-aliases/bulk` once when
   onboarding a new client file layout. They persist permanently for the organisation.

2. **Use `saveForReuse: true` on all mappings.** The second import with the same file
   layout requires no mapping step — the system loads saved mappings automatically.

3. **`duplicateStrategy: "update"` for recurring feeds; `"skip"` for initial loads.**
   Weekly or monthly refreshes from the same client should use `update` so corrections
   in the source data propagate. Use `skip` when loading new records only.

4. **Always preview before execute.** A preview costs nothing and catches mapping errors,
   missing required fields, and unexpected values before any data is written.

5. **Always check `errorRows` after `completed`.** A completed batch can still have
   row-level failures. Log the `/errors` response and reconcile skipped rows.

6. **`OriginalId` is the client's record identifier** — populate it from whatever the
   client uses as their internal customer ID (member number, student ID, etc.).
   It is stored as a plain string. `CustomerCode` is system-generated; never map to it.
