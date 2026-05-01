# CSV / Excel Import Design

Allows admin staff and ETL teams to upload customer data files
and map the file columns to the system's field definitions.

---

## Supported File Formats

| Format | Extension | Parser |
|---|---|---|
| CSV | `.csv` | CsvHelper |
| Excel 2007+ | `.xlsx` | ClosedXML |
| Excel 97-2003 | `.xls` | ClosedXML |

Both formats are handled identically after parsing — the file is
converted to an in-memory list of string arrays before any import
logic runs. The database tables are format-agnostic.

---

## Import Flow (5 steps)

```
Step 1: Upload
  Admin selects file + chooses which client it belongs to.
  Browser reads headers immediately (no server round-trip for headers).
  API creates ImportBatch record (status: pending).

Step 2: Auto-match
  System compares CSV headers against:
    - FieldDefinitions.FieldKey    (exact match)
    - FieldDefinitions.FieldLabel  (case-insensitive)
    - Customer column names:       FirstName, LastName, MiddleName, MaidenName,
                                   DateOfBirth, Email, Phone, OriginalId
    - Address column names:        AddressLine1, AddressLine2, City, State,
                                   PostalCode, Country, AddressType
  Each header is flagged as:
    ✓ Matched    — auto-matched, shown in green
    ⚠ Unmatched  — needs manual mapping, shown in amber
    — Skipped    — admin marks as not needed

Step 3: Map unmatched
  Each column mapping has three parts:
    1. Destination table   — Customer | Address | Key/Value | Skip
    2. Destination field   — field picker filtered by chosen table
    3. Transform           — Direct (1:1) | Split Full Name

  ── Customer fields ──────────────────
     FirstName, LastName, MiddleName, MaidenName, DateOfBirth,
     Email, Phone, OriginalId
     (CustomerCode excluded — system generated)

  ── Address fields ───────────────────
     AddressLine1, AddressLine2, City, State, PostalCode,
     Country, AddressType

  ── Key/Value fields (org-specific) ──
     Highest Degree, Phone Number, etc. — loaded from FieldDefinitions

  ── Skip ────────────────────────────
     Column is ignored during import

  Split Full Name transform:
    Select a source column containing a full name and choose
    "Split Full Name". Output tokens (FirstName, MiddleName,
    LastName, Suffix, Credentials) each get their own destination
    assignment in an expandable panel. Unneeded tokens can be
    set to Skip. The parser handles formats like:
      "Almena L. Free , M.D."  →  First=Almena  Middle=L  Last=Free  Credentials=M.D.
      "John Smith Jr."         →  First=John  Last=Smith  Suffix=Jr.
    State columns with full names ("Alabama") are automatically
    converted to 2-letter codes when mapped to the State address field.

  Admin cannot proceed until every header is mapped OR skipped.
  For dropdown/multiselect fields: optional value translation step
  (e.g. "Bachelor's Degree" → bach).

Step 4: Preview
  First 10 rows shown with mapping applied.
  Rows with missing required fields highlighted in red.
  Summary: N rows OK, N rows with warnings, N rows will error.
  Admin confirms to run the import.

Step 5: Import (server-side)
  For each data row:
    1. Generate CustomerCode from Org.Abbreviation + ULID
    2. Check for duplicate (by Email) — apply DuplicateStrategy
    3. INSERT into Customers (direct + split_full_name fields)
    4. INSERT into CustomerAddresses if any address field is mapped
       (requires at minimum AddressLine1 + City)
    5. INSERT/UPDATE FieldValues rows per mapped field_value column
    6. Write errors to ImportErrors for any failed rows
  Update ImportBatch counters and status on completion.
  Save successful mappings to SavedColumnMappings for reuse.
```

---

## CustomerCode Generation

`CustomerCode` is **always system-generated**. It is never mapped
from a CSV or Excel column.

**Format:** `{Abbreviation}-{ULID first 10 chars}`

**Examples:**
```
ACME-01ARZ3NDEK
BETA-01ARZ3NDFL
EC-01ARZ3NDFM
```

**Rules:**
- Requires `Organisation.Abbreviation` to be set (max 4 chars)
- The import service throws before processing if Abbreviation is missing
- Uniqueness guaranteed by `UNIQUE INDEX` on `Customers.CustomerCode`
- On collision (extremely rare with ULID): retry up to 5 times

**C# generator:**
```csharp
public static string Generate(string abbreviation)
{
    var prefix = abbreviation.ToUpperInvariant().Trim();
    var suffix = Ulid.NewUlid().ToString()[..10];
    return $"{prefix}-{suffix}";
}
```

---

## Duplicate Detection

Controlled by `ImportBatches.DuplicateStrategy`:

| Value | Behaviour |
|---|---|
| `skip` | Row is ignored if a customer with same Email already exists |
| `update` | Existing customer's FieldValues are updated with new data |
| `error` | Row is written to ImportErrors as a duplicate |

Matching is done by **Email** within the same Organisation.
If no Email column is mapped, the row is always treated as a new customer.

---

## Saved Mappings (Reuse)

After a successful import, column mappings flagged `SavedForReuse = 1`
are upserted into `SavedColumnMappings` keyed by:

```
OrganizationId + HeaderFingerprint
```

Where `HeaderFingerprint` = SHA-256 of the sorted, lowercased header list.

On next upload for the same org with identical headers:
- System finds the saved mapping
- Pre-populates all column mappings automatically
- Skips straight to Step 4 (Preview)

This means ETL teams who upload the same file format repeatedly
never need to re-map after the first time.

`UseCount` and `LastUsedAt` on `SavedColumnMappings` track how
often each mapping is used.

---

## Value Translation (Dropdown Fields)

For `dropdown` and `multiselect` fields, the CSV value must match
a valid `OptionKey` in `FieldOptions`. If the CSV uses different
text (e.g. long labels instead of short keys), `ImportValueMappings`
stores the translation.

**Example translations:**
```
CSV value                   →  OptionKey stored in FieldValues
───────────────────────────────────────────────────────────────
"Bachelor's Degree"         →  bach
"Bachelors"                 →  bach
"Masters"                   →  mast
"Master's Degree"           →  mast
"Yes" / "Y" / "TRUE" / "1" →  1    (checkbox fields)
"No"  / "N" / "FALSE"/ "0" →  0    (checkbox fields)
"California"                →  CA   (state dropdown)
"CA"                        →  CA   (already correct)
```

If no translation exists for a CSV value:
- The raw value is written to `ValueText` as-is
- The row is flagged as a warning (not an error)
- Admin can see these in the import results

---

## Import Tables

See `DATABASE.md` for full column definitions.

| Table | Purpose |
|---|---|
| `ImportBatches` | One per file upload, tracks lifecycle |
| `ImportColumnMappings` | Column-to-field mappings per batch |
| `ImportColumnMappingOutputs` | Per-token output assignments for split transforms |
| `ImportValueMappings` | Value translation rules per column |
| `ImportErrors` | Failed rows with error details |
| `SavedColumnMappings` | Reusable mappings per org + fingerprint |
| `SavedColumnMappingOutputs` | Saved per-token output assignments for split transforms |

---

## API Endpoints (planned)

```
POST /api/import/{orgId}/upload
  Body: multipart/form-data (file)
  Response: { batchId, headers[], rowCount, autoMappedCount, unmatchedCount }

GET /api/import/{orgId}/mappings?fingerprint={hash}
  Response: saved mappings if found, empty if none

POST /api/import/{batchId}/mappings
  Body: array of column mapping objects
  Response: 204

POST /api/import/{batchId}/preview
  Response: first 10 rows with mapping applied + summary stats

POST /api/import/{batchId}/execute
  Response: 202 Accepted (runs async)
  Poll: GET /api/import/{batchId} for status

GET /api/import/{orgId}/batches
  Response: paginated import history

GET /api/import/{batchId}/errors
  Response: failed rows, downloadable as corrected CSV
```

---

## NuGet Packages Required

```xml
<PackageReference Include="CsvHelper"  Version="33.0.1" />
<PackageReference Include="ClosedXML"  Version="0.104.2" />
```

---

## Admin UI Pages (planned)

### Import wizard (`/clients/:clientId/import`)
Five-step wizard:
1. File upload with drag/drop
2. Auto-match results — green/amber/grey per column
3. Manual mapping for unmatched columns + optional value translation
4. Preview table (first 10 rows)
5. Progress screen during execution + completion summary

### Import history (`/clients/:clientId/import/history`)
Table of past batches with:
- File name, upload date, uploaded by
- Status badge
- Row counts (imported / skipped / errors)
- Link to error details
- Re-import button (uses same mapping)
