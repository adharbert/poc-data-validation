# Marketing Flags, Gift Summary & Analytics Staging

> **Status: Planned — needs further discussion before implementation.**
> Architecture decisions are captured here. Final data element list from the
> analytics team is still pending. Do not begin building until that list is
> confirmed and a follow-up design review is done.

---

## Overview

When importing customer data, two additional data domains need to be captured
alongside core customer fields:

1. **Marketing Flags** — canonical opt-out and classification flags
2. **Gift Summary** — summarized donation history per customer

After import, a staging table in the main database holds the analytics-ready
version of these fields. The analytics team (separate database) pulls from or
is triggered by that staging table — no direct cross-database writes from the
import pipeline.

---

## Why Separate Tables (Not FieldValues)

Marketing flags and gift summary data must be in dedicated tables with fixed
columns — not the `FieldValues` EAV store. Reasons:

- Analytics queries need `WHERE DoNotEmail = 1` — unpivoting EAV rows at
  query time is expensive and fragile
- The analytics team's models depend on a consistent, predictable schema
  across all orgs
- Segmentation and export endpoints need fast boolean checks, not key lookups

---

## 1. Marketing Flags

### Canonical Flag Set

These are system-level fields, not org-specific `FieldDefinitions`. Every org
maps their client's columns to the same fixed set:

| Canonical Field | Type | Normalization Rule |
|---|---|---|
| `DoNotEmail` | `bit` | Any non-empty value = `1` |
| `DoNotPhone` | `bit` | Any non-empty value = `1` |
| `DoNotSolicit` | `bit` | Any non-empty value = `1` |
| `DoNotMail` | `bit` | Any non-empty value = `1` |
| `IsVIP` | `bit` | Any non-empty value = `1` |
| `IsBoardMember` | `bit` | Any non-empty value = `1` |
| `IsAlumni` | `bit` | Any non-empty value = `1` |
| `IsMember` | `bit` | Any non-empty value = `1` |

**Normalization rule:** If the column has any non-empty value — whether the
client uses `Y`, `1`, `true`, `Do Not Phone`, or anything else — the flag is
stored as `1`. The existing value alias system handles `Y/N`, `true/false`,
`1/0` → boolean before this rule applies.

### Proposed Table: `CustomerMarketingFlags`

```sql
CustomerMarketingFlagId  uniqueidentifier  PK
CustomerId               uniqueidentifier  FK → Customers
OrganizationId           uniqueidentifier  FK → Organizations
DoNotEmail               bit               default 0
DoNotPhone               bit               default 0
DoNotSolicit             bit               default 0
DoNotMail                bit               default 0
IsVIP                    bit               default 0
IsBoardMember            bit               default 0
IsAlumni                 bit               default 0
IsMember                 bit               default 0
ImportBatchId            uniqueidentifier  FK → ImportBatches (nullable)
CreatedAt                datetime2
UpdatedAt                datetime2
```

One row per customer. Upsert on re-import (update existing row, do not insert
a second).

---

## 2. Gift Summary

### Summary-Only Approach

Even when a client provides line-item donation records, the system collapses
them into a single summary row per customer. There is no line-item donation
history table. If the client file has multiple donation rows for the same
customer, they are summed/aggregated during import.

Fields captured:

| Field | Type | Notes |
|---|---|---|
| `LastGiftAmount` | `decimal(18,2)` | Most recent gift dollar value |
| `LastGiftDate` | `date` | Most recent gift date |
| `TotalGiftAmount` | `decimal(18,2)` | Sum of all gifts |
| `NumberOfGifts` | `int` | Count of individual gifts |
| `LargestGiftAmount` | `decimal(18,2)` | Single largest gift (optional — TBD) |
| `FirstGiftDate` | `date` | Earliest known gift date (optional — TBD) |

> **Note:** `LargestGiftAmount` and `FirstGiftDate` are listed as optional.
> Confirm with analytics team whether these are needed for their models.

### Proposed Table: `CustomerGiftSummary`

```sql
GiftSummaryId       uniqueidentifier  PK
CustomerId          uniqueidentifier  FK → Customers
OrganizationId      uniqueidentifier  FK → Organizations
LastGiftAmount      decimal(18,2)     nullable
LastGiftDate        date              nullable
TotalGiftAmount     decimal(18,2)     nullable
NumberOfGifts       int               nullable
LargestGiftAmount   decimal(18,2)     nullable
FirstGiftDate       date              nullable
ImportBatchId       uniqueidentifier  FK → ImportBatches (nullable)
CreatedAt           datetime2
UpdatedAt           datetime2
```

One row per customer. Upsert on re-import.

---

## 3. Analytics Staging Table

### Approach

Instead of writing directly to the analytics team's database, a staging table
lives in the **main application database**. This decouples the import pipeline
from the analytics database entirely.

Benefits:
- Import completes successfully even if the analytics database is unavailable
- Analytics team can pull on their own schedule, or a trigger/job can push
- Data can be reviewed and corrected before it flows downstream
- Connection string changes (Azure migration) only affect the analytics side

### Proposed Table: `AnalyticsCustomerStaging`

```sql
StagingId             uniqueidentifier  PK
CustomerId            uniqueidentifier  FK → Customers
CustomerCode          varchar(20)
OrganizationId        uniqueidentifier  FK → Organizations
-- Core demographics
FirstName             nvarchar(100)
LastName              nvarchar(100)
Email                 nvarchar(255)
DateOfBirth           date              nullable
-- Marketing flags (denormalized for flat analytics schema)
DoNotEmail            bit
DoNotPhone            bit
DoNotSolicit          bit
DoNotMail             bit
IsVIP                 bit
IsBoardMember         bit
IsAlumni              bit
IsMember              bit
-- Gift summary (denormalized)
LastGiftAmount        decimal(18,2)     nullable
LastGiftDate          date              nullable
TotalGiftAmount       decimal(18,2)     nullable
NumberOfGifts         int               nullable
-- Staging metadata
ImportBatchId         uniqueidentifier  FK → ImportBatches
StagedAt              datetime2
SyncedToAnalyticsAt   datetime2         nullable  ← set when pushed to analytics DB
SyncStatus            varchar(20)       -- 'pending' | 'synced' | 'error'
SyncError             nvarchar(max)     nullable
```

### Sync Mechanism (TBD — needs discussion)

Two options; which one to use depends on the analytics team's preference:

| Option | How it works |
|---|---|
| **Pull** | Analytics team queries `AnalyticsCustomerStaging WHERE SyncStatus = 'pending'` on their own schedule |
| **Push** | A background job or SQL trigger moves rows to the analytics DB and sets `SyncedToAnalyticsAt` |

Both are compatible with the staging table design. This decision does not need
to be made before the staging table is built.

---

## 4. Import Pipeline Changes

### Column Mapping — New Destinations

The import wizard Step 3 (Map Columns) gains two new destination options:

| Destination | Field picker shows |
|---|---|
| **Marketing Flags** | DoNotEmail, DoNotPhone, DoNotSolicit, DoNotMail, IsVIP, IsBoardMember, IsAlumni, IsMember |
| **Gift Summary** | LastGiftAmount, LastGiftDate, TotalGiftAmount, NumberOfGifts, LargestGiftAmount, FirstGiftDate |

A new transform option **"Any value = flagged"** is added for Marketing Flag
mappings — the importer treats any non-empty cell value as `true` regardless
of what the client put in the column.

### Background Worker — Additional Steps

After the existing customer insert/update steps, the worker adds:

1. Upsert `CustomerMarketingFlags` row for this customer
2. Upsert `CustomerGiftSummary` row for this customer
3. Upsert `AnalyticsCustomerStaging` row for this customer (status: `pending`)

All three steps are within the same per-batch transaction scope. A failure in
steps 1–3 is logged as a warning on the batch but does not mark the row as a
customer import error.

---

## 5. Post-Import Review (Planned UI)

After an import completes, admins need to be able to:

- View the marketing flags and gift summary that were captured for each customer
- Edit/correct values before they are synced to analytics
- See `SyncStatus` per customer (pending / synced / error)

This would live as a new tab or panel on the **Import Results** screen and/or
the **Customer Detail** page.

> **Needs discussion:** Exact UI placement and whether corrections to flags/gift
> data re-trigger the staging row update.

---

## Open Questions (Discuss Before Building)

1. **Final analytics data element list** — analytics team has not confirmed the
   full column list they need in `AnalyticsCustomerStaging`. Do not finalize
   the staging table schema until this is received.

2. **Additional canonical flags** — are there marketing flags beyond the 8
   listed? Confirm with marketing team.

3. **`LargestGiftAmount` / `FirstGiftDate`** — are these needed for donor
   models? Confirm with analytics team.

4. **Sync mechanism** — push job or pull? Confirm with analytics team.

5. **Re-import behavior** — if a customer is re-imported, do gift summary
   fields overwrite or merge (take the higher total, the more recent date)?

6. **Multi-file imports** — if a client sends a separate donation file (not
   embedded in the customer file), is a separate upload step needed, or will
   clients always provide summary columns in the main file?

---

## Build Order (When Ready)

1. SSDT: add `CustomerMarketingFlags`, `CustomerGiftSummary`,
   `AnalyticsCustomerStaging` tables
2. API: repositories + services for all three tables
3. Import: add Marketing Flags + Gift Summary as column mapping destinations
4. Import: background worker writes all three tables after customer rows
5. UI: post-import review panel on Import Results and Customer Detail page
6. Analytics sync: push job or pull endpoint (after sync mechanism is decided)
