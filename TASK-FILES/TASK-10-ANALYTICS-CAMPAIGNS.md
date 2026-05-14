# Analytics, Campaigns & Canonical Data Standardization

Phase 4 — builds on validated customer data to drive scoring, targeting,
and multi-channel campaign execution.

---

## Business Flow

```
Customer data validated (Phase 1–3)
        │
        ▼
Analytics team scores customers
  → Buyer model score + decile (1–10)
  → Donor model score + decile (1–10)
        │
        ▼
Decile thresholds set per MarketingProject
  → Determine which customers get which channel
        │
        ├── Phone     → Five9 contact list export
        ├── Email     → Iterable subscriber export
        ├── Postcard  → Mailing vendor address file
        └── Suppress  → No outreach
        │
        ▼
Results feed back into next scoring cycle
```

---

## Architectural Boundaries

| Layer | Entity | Responsibility |
|---|---|---|
| Policy | `Organization` | Rules that apply across ALL projects for this org (global suppression, channel restrictions, compliance flags) |
| Execution | `MarketingProject` | All campaign activity — segmentations, channel assignments, vendor exports, scoring runs |
| Reference | `CanonicalFieldTypes` / `CanonicalFieldValues` | System-wide standardized taxonomy for cross-org modeling and reporting |

---

## Canonical Data Standardization

### The Problem

Each org defines their own field names and option values for the same
real-world concepts:

| Org A field | Org A options | Org B field | Org B options |
|---|---|---|---|
| "Education Level" | HS Diploma, Some College, Bachelor's | "Highest Schooling" | High School, 2yr Degree, 4yr Degree |
| "Income Range" | Under 30k, 30–60k, 60k+ | "Household Income" | Low, Middle, High |

The analytics models and standardized reports need a consistent representation
regardless of which org a customer came from.

### Solution: Canonical Taxonomy + Mapping Layer

Normalisation happens **at export/report time** — raw values are preserved
as-is in `FieldValues`; the canonical mapping is a lookup layer on top.
This means updating a mapping fixes all historical data automatically.

### Key Canonical Field Types (initial set)

| TypeKey | DisplayName | Notes |
|---|---|---|
| `education_level` | Education Level | See standard values below |
| `income_bracket` | Household Income Bracket | Dollar range bands |
| `homeownership_status` | Homeownership Status | Own / Rent / Other |
| `marital_status` | Marital Status | |
| `age_range` | Age Range | Decade bands (18–24, 25–34, …) |
| `gender` | Gender | |
| `military_status` | Military / Veteran Status | |
| `employment_status` | Employment Status | |
| `donation_frequency` | Donation Frequency | For donor-model orgs |

### Standard Education Values

| ValueKey | Display Label |
|---|---|
| `less_than_hs` | Less Than High School |
| `hs_diploma` | High School Diploma / GED |
| `some_college` | Some College, No Degree |
| `associates` | Associate's Degree |
| `bachelors` | Bachelor's Degree |
| `graduate` | Graduate / Professional Degree |
| `unknown` | Unknown / Not Provided |

### Standard Income Bracket Values

| ValueKey | Display Label |
|---|---|
| `under_25k` | Under $25,000 |
| `25k_50k` | $25,000 – $49,999 |
| `50k_75k` | $50,000 – $74,999 |
| `75k_100k` | $75,000 – $99,999 |
| `100k_150k` | $100,000 – $149,999 |
| `150k_plus` | $150,000 and Over |
| `unknown` | Unknown / Not Provided |

---

## Database Schema

### CanonicalFieldTypes

```sql
CREATE TABLE dbo.CanonicalFieldTypes (
    [Id]            int             NOT NULL IDENTITY(1,1),
    [TypeKey]       nvarchar(50)    NOT NULL,   -- 'education_level', 'income_bracket', etc.
    [DisplayName]   nvarchar(100)   NOT NULL,
    [DataType]      nvarchar(20)    NOT NULL,   -- 'option' | 'range' | 'boolean'
    [IsActive]      bit             NOT NULL DEFAULT 1,
    CONSTRAINT [PK_CanonicalFieldTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_CanonicalFieldTypes_Key] UNIQUE ([TypeKey])
)
```

### CanonicalFieldValues

```sql
CREATE TABLE dbo.CanonicalFieldValues (
    [Id]                    int             NOT NULL IDENTITY(1,1),
    [CanonicalFieldTypeId]  int             NOT NULL,
    [ValueKey]              nvarchar(50)    NOT NULL,   -- 'hs_diploma', 'bachelors', etc.
    [DisplayLabel]          nvarchar(100)   NOT NULL,
    [SortOrder]             int             NOT NULL DEFAULT 0,
    CONSTRAINT [PK_CanonicalFieldValues] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_CanonicalFieldValues_Key] UNIQUE ([CanonicalFieldTypeId], [ValueKey]),
    CONSTRAINT [FK_CanonicalFieldValues_Type] FOREIGN KEY ([CanonicalFieldTypeId])
        REFERENCES dbo.CanonicalFieldTypes ([Id])
)
```

### FieldDefinitionCanonicalMapping

Links an org's `FieldDefinition` to a `CanonicalFieldType`.
One admin maps this once; all orgs using "education level" point to the same type.

```sql
CREATE TABLE dbo.FieldDefinitionCanonicalMapping (
    [FieldDefinitionId]     uniqueidentifier    NOT NULL,
    [CanonicalFieldTypeId]  int                 NOT NULL,
    [MappedBy]              nvarchar(200)       NOT NULL,
    [MappedAt]              datetime2           NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_FieldDefinitionCanonicalMapping] PRIMARY KEY ([FieldDefinitionId]),
    CONSTRAINT [FK_FDCM_FieldDef] FOREIGN KEY ([FieldDefinitionId])
        REFERENCES dbo.FieldDefinitions ([FieldDefinitionId]),
    CONSTRAINT [FK_FDCM_CanonicalType] FOREIGN KEY ([CanonicalFieldTypeId])
        REFERENCES dbo.CanonicalFieldTypes ([Id])
)
```

### FieldOptionCanonicalMapping

Maps each org's option value to a canonical value within the type.

```sql
CREATE TABLE dbo.FieldOptionCanonicalMapping (
    [FieldOptionId]         uniqueidentifier    NOT NULL,
    [CanonicalFieldValueId] int                 NOT NULL,
    [MappedBy]              nvarchar(200)       NOT NULL,
    [MappedAt]              datetime2           NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_FieldOptionCanonicalMapping] PRIMARY KEY ([FieldOptionId]),
    CONSTRAINT [FK_FOCM_Option] FOREIGN KEY ([FieldOptionId])
        REFERENCES dbo.FieldOptions ([FieldOptionId]),
    CONSTRAINT [FK_FOCM_CanonicalValue] FOREIGN KEY ([CanonicalFieldValueId])
        REFERENCES dbo.CanonicalFieldValues ([Id])
)
```

### CustomerScores

Analytics team sends back model scores per customer per model run.
Keyed on `CustomerCode` for import; stored with `CustomerId` internally.

```sql
CREATE TABLE dbo.CustomerScores (
    [Id]                uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [CustomerId]        uniqueidentifier    NOT NULL,
    [OrganizationId]    uniqueidentifier    NOT NULL,
    [ModelType]         nvarchar(20)        NOT NULL,   -- 'buyer' | 'donor'
    [ModelVersion]      nvarchar(50)        NOT NULL,   -- '2025-Q1', etc.
    [Score]             decimal(10,6)       NOT NULL,   -- raw model probability
    [Decile]            tinyint             NOT NULL,   -- 1 (top) – 10 (bottom)
    [ScoredAt]          datetime2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [ScoredBy]          nvarchar(200)       NOT NULL,   -- analytics job / team identifier
    CONSTRAINT [PK_CustomerScores] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerScores_Customer] FOREIGN KEY ([CustomerId])
        REFERENCES dbo.Customers ([CustomerId]),
    CONSTRAINT [CK_CustomerScores_Decile] CHECK ([Decile] BETWEEN 1 AND 10),
    CONSTRAINT [CK_CustomerScores_ModelType] CHECK ([ModelType] IN ('buyer', 'donor'))
)
CREATE INDEX [IX_CustomerScores_Customer] ON dbo.CustomerScores ([CustomerId], [ModelType], [ScoredAt] DESC)
CREATE INDEX [IX_CustomerScores_Org]      ON dbo.CustomerScores ([OrganizationId], [ModelType], [Decile])
```

### ProjectChannelRules

Decile thresholds set per project that determine which customers get which channel.

```sql
CREATE TABLE dbo.ProjectChannelRules (
    [Id]                int             NOT NULL IDENTITY(1,1),
    [ProjectId]         int             NOT NULL,
    [ModelType]         nvarchar(20)    NOT NULL,   -- 'buyer' | 'donor'
    [Channel]           nvarchar(20)    NOT NULL,   -- 'phone' | 'email' | 'postcard' | 'suppress'
    [DecileMin]         tinyint         NOT NULL,   -- inclusive lower bound (1 = top)
    [DecileMax]         tinyint         NOT NULL,   -- inclusive upper bound
    CONSTRAINT [PK_ProjectChannelRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PCR_Project] FOREIGN KEY ([ProjectId])
        REFERENCES dbo.MarketingProjects ([ProjectId]),
    CONSTRAINT [CK_PCR_Channel] CHECK ([Channel] IN ('phone', 'email', 'postcard', 'suppress')),
    CONSTRAINT [CK_PCR_DecileRange] CHECK ([DecileMin] <= [DecileMax] AND [DecileMin] >= 1 AND [DecileMax] <= 10)
)
```

### OrganizationCampaignPolicy

Org-level rules that override or constrain campaign execution across all projects.

```sql
CREATE TABLE dbo.OrganizationCampaignPolicy (
    [OrganizationId]        uniqueidentifier    NOT NULL,
    [AllowPhone]            bit                 NOT NULL DEFAULT 1,
    [AllowEmail]            bit                 NOT NULL DEFAULT 1,
    [AllowPostcard]         bit                 NOT NULL DEFAULT 1,
    [GlobalDncListPath]     nvarchar(500)       NULL,   -- path to org-wide DNC file
    [Notes]                 nvarchar(1000)      NULL,
    [UpdatedAt]             datetime2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedBy]             nvarchar(200)       NOT NULL,
    CONSTRAINT [PK_OrgCampaignPolicy] PRIMARY KEY ([OrganizationId]),
    CONSTRAINT [FK_OCP_Org] FOREIGN KEY ([OrganizationId])
        REFERENCES dbo.Organizations ([OrganizationId])
)
```

### ProjectVendorExports

Audit trail of what was pushed to each vendor and when.

```sql
CREATE TABLE dbo.ProjectVendorExports (
    [Id]                    uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [ProjectId]             int                 NOT NULL,
    [SegmentationId]        uniqueidentifier    NULL,   -- NULL = full project export
    [VendorType]            nvarchar(20)        NOT NULL,   -- 'five9' | 'iterable' | 'mailing'
    [ExportedAt]            datetime2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [ExportedBy]            nvarchar(200)       NOT NULL,
    [RecordCount]           int                 NOT NULL,
    [VendorReferenceId]     nvarchar(200)       NULL,   -- Five9 list ID, Iterable campaign ID, etc.
    [FilePath]              nvarchar(500)       NULL,   -- for mailing vendor file exports
    CONSTRAINT [PK_ProjectVendorExports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PVE_Project] FOREIGN KEY ([ProjectId])
        REFERENCES dbo.MarketingProjects ([ProjectId]),
    CONSTRAINT [CK_PVE_Vendor] CHECK ([VendorType] IN ('five9', 'iterable', 'mailing'))
)
```

---

## Reporting Standardization

Reports that compare data across organisations use the canonical mapping layer.
Raw `FieldValues` are never aggregated directly — always joined through to canonical values.

### View: vCustomerCanonicalValues

```sql
CREATE VIEW dbo.vCustomerCanonicalValues AS
SELECT
    fv.CustomerId,
    c.OrganizationId,
    c.CustomerCode,
    fd.FieldDefinitionId,
    cft.TypeKey                 AS CanonicalFieldType,
    cft.DisplayName             AS CanonicalFieldName,
    cfv.ValueKey                AS CanonicalValueKey,
    cfv.DisplayLabel            AS CanonicalValueLabel,
    fv.FieldValue               AS RawValue
FROM dbo.FieldValues fv
JOIN dbo.Customers c                        ON c.CustomerId = fv.CustomerId
JOIN dbo.FieldDefinitions fd                ON fd.FieldDefinitionId = fv.FieldDefinitionId
JOIN dbo.FieldDefinitionCanonicalMapping fdcm ON fdcm.FieldDefinitionId = fd.FieldDefinitionId
JOIN dbo.CanonicalFieldTypes cft            ON cft.Id = fdcm.CanonicalFieldTypeId
LEFT JOIN dbo.FieldOptions fo               ON fo.FieldDefinitionId = fd.FieldDefinitionId
                                           AND fo.OptionValue = fv.FieldValue
LEFT JOIN dbo.FieldOptionCanonicalMapping focm ON focm.FieldOptionId = fo.FieldOptionId
LEFT JOIN dbo.CanonicalFieldValues cfv      ON cfv.Id = focm.CanonicalFieldValueId
```

This view enables cross-org reports like:
- "Education level distribution across all clients"
- "Income bracket breakdown per org"
- "Decile 1–3 buyer model customers by education level"

---

## Vendor Integration Notes

### Five9 (Phone — Outbound & Inbound)

- **Contact list format:** CSV with phone number + name + `CustomerCode` + any custom data fields the campaign needs
- **Outbound campaigns:** Predictive, progressive, preview, or power dialing modes
- **Inbound:** Handled via Five9 IVR; results linked back to customer via `CustomerCode`
- **API:** Five9 REST API (`/lists`) for programmatic contact list push/update
- **Result matching:** Five9 returns call disposition records keyed on the external reference field — use `CustomerCode`

### Iterable (Email Marketing)

- **Subscriber key:** `CustomerCode` as the unique identifier
- **Data fields:** Email address + name + canonical field values for personalisation/segmentation
- **Campaigns:** Iterable campaign ID stored in `ProjectVendorExports.VendorReferenceId`
- **API:** Iterable REST API for list upload and event triggering

### Mailing Vendor (Postcards)

- **Export format:** CSV with full address block — FirstName, LastName, Address, City, State, Zip
- **Reference field:** `CustomerCode` on the file for return/match purposes
- **Address source:** `CustomerAddresses` table (`IsCurrent = 1` address for the customer)

---

## Analytics Team Data Exchange

All data exchanged with the analytics team uses `CustomerCode` as the primary identifier.
`CustomerId` (GUID) may be included as a secondary reference for system lookups.
`OriginalId` is never used in analytics exchange — it is inbound/ETL only.

### Export to analytics team

Minimum fields per customer row:
- `CustomerCode` — primary key for the exchange
- `OrganizationId` / org abbreviation — to distinguish clients
- All canonical field values (via `vCustomerCanonicalValues`)
- Address-derived fields (state, ZIP) for geographic modelling

### Import scores back from analytics team

CSV keyed on `CustomerCode`:
```
CustomerCode, ModelType, ModelVersion, Score, Decile
ADX-00001,    buyer,     2025-Q2,      0.847, 1
ADX-00002,    buyer,     2025-Q2,      0.341, 6
```

API endpoint receives the file, resolves `CustomerCode` → `CustomerId`, inserts into `CustomerScores`.

---

## API Routes (planned)

```
Canonical Taxonomy (system admin only):
  GET    /api/canonical-field-types
  GET    /api/canonical-field-types/{typeId}/values
  POST   /api/canonical-field-types
  POST   /api/canonical-field-types/{typeId}/values

Canonical Mapping (org admin):
  GET    /api/organisations/{orgId}/canonical-mappings
  PUT    /api/organisations/{orgId}/fields/{fieldId}/canonical-mapping
  PUT    /api/organisations/{orgId}/field-options/{optionId}/canonical-mapping

Customer Scores:
  POST   /api/organisations/{orgId}/scores/import       (CSV upload, keyed on CustomerCode)
  GET    /api/organisations/{orgId}/scores?modelType=buyer&modelVersion=2025-Q2
  GET    /api/organisations/{orgId}/customers/{customerId}/scores

Organisation Campaign Policy:
  GET    /api/organisations/{orgId}/campaign-policy
  PUT    /api/organisations/{orgId}/campaign-policy

Project Channel Rules:
  GET    /api/organisations/{orgId}/projects/{projectId}/channel-rules
  PUT    /api/organisations/{orgId}/projects/{projectId}/channel-rules

Vendor Exports:
  POST   /api/organisations/{orgId}/projects/{projectId}/exports/five9
  POST   /api/organisations/{orgId}/projects/{projectId}/exports/iterable
  POST   /api/organisations/{orgId}/projects/{projectId}/exports/mailing
  GET    /api/organisations/{orgId}/projects/{projectId}/exports

Standardised Reports:
  GET    /api/organisations/{orgId}/reports/canonical-distribution?fieldType=education_level
  GET    /api/reports/cross-org/canonical-distribution?fieldType=education_level
  GET    /api/organisations/{orgId}/reports/decile-distribution?modelType=buyer
```

---

## Phase 4 Build Order

1. **Canonical taxonomy** — `CanonicalFieldTypes` + `CanonicalFieldValues` tables + seed data + admin UI to manage
2. **Canonical mappings** — `FieldDefinitionCanonicalMapping` + `FieldOptionCanonicalMapping` + mapping UI per org
3. **`vCustomerCanonicalValues` view** — enables all cross-org reporting
4. **Customer scores** — `CustomerScores` table + CSV import endpoint (keyed on `CustomerCode`)
5. **Org campaign policy** — `OrganizationCampaignPolicy` table + API + admin UI
6. **Project channel rules** — `ProjectChannelRules` table + API + project UI
7. **Vendor export endpoints** — Five9, Iterable, mailing; each shaped to vendor's format
8. **Vendor export audit** — `ProjectVendorExports` table wired into each export endpoint
9. **Standardised reports** — canonical distribution, decile distribution, cross-org views
```
