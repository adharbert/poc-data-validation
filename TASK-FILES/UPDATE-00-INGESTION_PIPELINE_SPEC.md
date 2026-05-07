# AI-Assisted Customer Data Ingestion Pipeline
## Technical Specification

**Stack:** .NET 10 (C#) · React JS · SQL Server · Azure · Azure DevOps  
**Scope:** Multitenant ETL pipeline for client-submitted CSV/XLSX customer data  
**Status:** In progress — customer/org tables exist; file upload flow needs rebuild

---

## 1. Overview

Clients submit CSV or Excel files containing customer data in non-standard formats.
The pipeline uses AI to infer column mappings, normalize data, validate addresses via
Melissa, and populate the customer tables. Business users can review flagged rows without
ETL team involvement for smaller clients.

### Tenancy model

Every record is scoped to an `OrganizationId`. The pipeline enforces this at every
layer — file intake, transformation, validation, and final insert.

---

## 2. Database Schema (SQL Server — Visual Studio DB Project)

All scripts go in the `.sqlproj` as `Post-Deployment` scripts or as numbered migration
objects so they apply cleanly to every environment.

### 2.1 Core tables (add if not yet present)

```sql
-- Ingestion job tracker
CREATE TABLE [dbo].[IngestionJob] (
    [IngestionJobId]    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [OrganizationId]    UNIQUEIDENTIFIER    NOT NULL,
    [FileName]          NVARCHAR(500)       NOT NULL,
    [FileSizeBytes]     BIGINT              NOT NULL,
    [FileHash]          NVARCHAR(64)        NOT NULL,   -- SHA-256, dedup guard
    [UploadedByUserId]  UNIQUEIDENTIFIER    NOT NULL,
    [UploadedAt]        DATETIMEOFFSET      NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Status]            NVARCHAR(50)        NOT NULL DEFAULT 'Pending',
        -- Pending | Classifying | Mapping | Normalizing | Validating
        -- | AwaitingReview | Committing | Complete | Failed
    [Tier]              NVARCHAR(20)        NULL,       -- Auto | Review | ETL
    [TotalRows]         INT                 NULL,
    [PassedRows]        INT                 NULL,
    [FlaggedRows]       INT                 NULL,
    [FailedRows]        INT                 NULL,
    [ErrorMessage]      NVARCHAR(MAX)       NULL,
    [CompletedAt]       DATETIMEOFFSET      NULL,
    CONSTRAINT [PK_IngestionJob] PRIMARY KEY CLUSTERED ([IngestionJobId])
);

-- AI-generated column mapping for each job
CREATE TABLE [dbo].[IngestionColumnMap] (
    [IngestionColumnMapId]  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [IngestionJobId]        UNIQUEIDENTIFIER    NOT NULL,
    [SourceColumn]          NVARCHAR(200)       NOT NULL,
    [MappedToField]         NVARCHAR(200)       NULL,   -- e.g. "Customer.FirstName"
    [TransformHint]         NVARCHAR(500)       NULL,   -- e.g. "parse as MM/DD/YY"
    [ConfidenceScore]       DECIMAL(5,4)        NULL,   -- 0.0000–1.0000
    [IsApproved]            BIT                 NOT NULL DEFAULT 0,
    [ApprovedByUserId]      UNIQUEIDENTIFIER    NULL,
    [ApprovedAt]            DATETIMEOFFSET      NULL,
    CONSTRAINT [PK_IngestionColumnMap] PRIMARY KEY CLUSTERED ([IngestionColumnMapId]),
    CONSTRAINT [FK_IngestionColumnMap_Job]
        FOREIGN KEY ([IngestionJobId]) REFERENCES [dbo].[IngestionJob]([IngestionJobId])
);

-- Approved mapping templates — reused for known clients
CREATE TABLE [dbo].[IngestionMappingTemplate] (
    [TemplateId]        UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [OrganizationId]    UNIQUEIDENTIFIER    NOT NULL,
    [TemplateName]      NVARCHAR(200)       NOT NULL,
    [SchemaFingerprint] NVARCHAR(64)        NULL,   -- hash of sorted source column names
    [MappingJson]       NVARCHAR(MAX)       NOT NULL,  -- serialized column map array
    [CreatedAt]         DATETIMEOFFSET      NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [LastUsedAt]        DATETIMEOFFSET      NULL,
    CONSTRAINT [PK_IngestionMappingTemplate] PRIMARY KEY CLUSTERED ([TemplateId])
);

-- Staging area — holds transformed rows before commit
CREATE TABLE [dbo].[IngestionStagingRow] (
    [StagingRowId]      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [IngestionJobId]    UNIQUEIDENTIFIER    NOT NULL,
    [RowNumber]         INT                 NOT NULL,
    [RowJson]           NVARCHAR(MAX)       NOT NULL,   -- normalized field values
    [ConfidenceScore]   DECIMAL(5,4)        NULL,
    [Status]            NVARCHAR(50)        NOT NULL DEFAULT 'Pending',
        -- Pending | Pass | Flagged | Rejected | Committed
    [FlagReasons]       NVARCHAR(MAX)       NULL,   -- JSON array of reason strings
    [ReviewedByUserId]  UNIQUEIDENTIFIER    NULL,
    [ReviewedAt]        DATETIMEOFFSET      NULL,
    [MelissaResult]     NVARCHAR(MAX)       NULL,   -- raw Melissa response JSON
    CONSTRAINT [PK_IngestionStagingRow] PRIMARY KEY CLUSTERED ([StagingRowId]),
    CONSTRAINT [FK_IngestionStagingRow_Job]
        FOREIGN KEY ([IngestionJobId]) REFERENCES [dbo].[IngestionJob]([IngestionJobId])
);

-- Per-org dynamic attribute definitions (the client key/value config)
CREATE TABLE [dbo].[OrgAttributeDefinition] (
    [AttributeDefinitionId] UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [OrganizationId]        UNIQUEIDENTIFIER    NOT NULL,
    [AttributeKey]          NVARCHAR(100)       NOT NULL,
    [DisplayName]           NVARCHAR(200)       NOT NULL,
    [DataType]              NVARCHAR(50)        NOT NULL,   -- String|Int|Decimal|Date|Bool
    [ValidationRule]        NVARCHAR(500)       NULL,       -- regex or range expression
    [IsRequired]            BIT                 NOT NULL DEFAULT 0,
    [SortOrder]             INT                 NOT NULL DEFAULT 0,
    CONSTRAINT [PK_OrgAttributeDefinition] PRIMARY KEY CLUSTERED ([AttributeDefinitionId]),
    CONSTRAINT [UQ_OrgAttribute] UNIQUE ([OrganizationId], [AttributeKey])
);

-- Customer dynamic attribute values
CREATE TABLE [dbo].[CustomerAttribute] (
    [CustomerAttributeId]   UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [CustomerId]            UNIQUEIDENTIFIER    NOT NULL,
    [OrganizationId]        UNIQUEIDENTIFIER    NOT NULL,
    [AttributeKey]          NVARCHAR(100)       NOT NULL,
    [AttributeValue]        NVARCHAR(MAX)       NULL,
    CONSTRAINT [PK_CustomerAttribute] PRIMARY KEY CLUSTERED ([CustomerAttributeId]),
    CONSTRAINT [UQ_CustomerAttribute] UNIQUE ([CustomerId], [AttributeKey])
);

-- LLM model feature store (propensity signals)
CREATE TABLE [dbo].[CustomerModelFeature] (
    [CustomerModelFeatureId]    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    [CustomerId]                UNIQUEIDENTIFIER    NOT NULL,
    [OrganizationId]            UNIQUEIDENTIFIER    NOT NULL,
    [FeatureKey]                NVARCHAR(200)       NOT NULL,
    [FeatureValue]              NVARCHAR(MAX)       NULL,
    [ExtractedAt]               DATETIMEOFFSET      NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT [PK_CustomerModelFeature] PRIMARY KEY CLUSTERED ([CustomerModelFeatureId])
);
```

### 2.2 Indexes to add

```sql
CREATE INDEX [IX_IngestionJob_Org_Status]
    ON [dbo].[IngestionJob] ([OrganizationId], [Status]);

CREATE INDEX [IX_IngestionStagingRow_Job_Status]
    ON [dbo].[IngestionStagingRow] ([IngestionJobId], [Status]);

CREATE INDEX [IX_CustomerAttribute_CustomerId]
    ON [dbo].[CustomerAttribute] ([CustomerId], [OrganizationId]);
```

---

## 3. Backend — .NET 10 C# API

### 3.1 Project structure (within monorepo root)

```
/Api
  /Controllers
    IngestionController.cs
  /Services
    IngestionJobService.cs
    AiMappingService.cs          ← calls Claude API
    DataNormalizationService.cs
    MelissaValidationService.cs
    IngestionCommitService.cs
  /Models
    IngestionJob.cs
    IngestionColumnMap.cs
    StagingRow.cs
    MappingTemplate.cs
  /BackgroundJobs
    IngestionProcessorJob.cs     ← Hangfire or hosted service
  /Hubs
    IngestionProgressHub.cs      ← SignalR for real-time UI updates
```

### 3.2 File upload endpoint

```csharp
[ApiController]
[Route("api/ingestion")]
[Authorize]
public class IngestionController : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromServices] IngestionJobService svc,
        CancellationToken ct)
    {
        var orgId = User.GetOrganizationId(); // extension on ClaimsPrincipal
        var job = await svc.CreateJobAsync(orgId, file, ct);
        return Accepted(new { job.IngestionJobId, job.Status });
    }

    [HttpGet("{jobId}/status")]
    public async Task<IActionResult> GetStatus(Guid jobId, ...) { ... }

    [HttpGet("{jobId}/staging")]
    public async Task<IActionResult> GetStagingRows(Guid jobId, 
        string? statusFilter, int page = 1, int pageSize = 50, ...) { ... }

    [HttpPost("{jobId}/staging/{rowId}/approve")]
    public async Task<IActionResult> ApproveRow(Guid jobId, Guid rowId, ...) { ... }

    [HttpPost("{jobId}/commit")]
    public async Task<IActionResult> CommitJob(Guid jobId, ...) { ... }
}
```

### 3.3 AI mapping service

This service sends a sample of the source file to the Claude API and receives a
structured mapping back. Use `claude-sonnet-4-20250514` (or latest Sonnet 4).

```csharp
public class AiMappingService
{
    // Target schema description passed in the system prompt
    private const string TargetSchemaDescription = """
        Target fields available for mapping:
        - Customer.FirstName (string)
        - Customer.LastName (string)
        - Customer.Email (string, must be valid email)
        - Customer.DateOfBirth (date)
        - CustomerPhone.PhoneNumber (E.164 format)
        - CustomerPhone.PhoneType (Mobile|Home|Work)
        - CustomerAddress.Line1 (string)
        - CustomerAddress.Line2 (string, nullable)
        - CustomerAddress.City (string)
        - CustomerAddress.State (2-letter abbreviation)
        - CustomerAddress.PostalCode (5 or 9 digit ZIP)
        - CustomerAddress.Country (ISO 3166-1 alpha-2, default US)
        - CustomerAttribute.[key] (dynamic per org — keys provided below)
        """;

    public async Task<List<ColumnMapping>> InferMappingsAsync(
        string[] sourceColumns,
        string[][] sampleRows,          // first 5 rows
        List<OrgAttributeDefinition> orgAttributes,
        MappingTemplate? priorTemplate) // reuse if schema fingerprint matches
    {
        var prompt = BuildMappingPrompt(sourceColumns, sampleRows, orgAttributes, priorTemplate);
        // Call Anthropic API, parse JSON response, return List<ColumnMapping>
    }
}
```

**Prompt structure for the mapping call:**

```
System:
You are a data mapping assistant. Given source columns and sample data from a 
client CSV/Excel file, return a JSON array mapping each source column to the 
correct target field. Include a confidence score (0–1) and a transform_hint if 
the value needs normalization (e.g., date format, phone cleanup).

Respond ONLY with a JSON array. No prose, no markdown fences.

Schema: {TargetSchemaDescription}
Org-specific attributes: {orgAttributeList}

User:
Source columns: ["dob","fname","lname","cell","addr1","zip"]
Sample rows:
  Row 1: ["01/15/85","John","Smith","800-555-1234","123 Main St","90210"]
  Row 2: ["03/22/90","Jane","Doe","(310)555-9876","456 Oak Ave","10001"]

Prior approved mapping for this org (use as strong signal):
{priorMappingJson or "none"}
```

**Expected response:**

```json
[
  { "source_column": "fname", "target_field": "Customer.FirstName", "confidence": 0.99, "transform_hint": null },
  { "source_column": "lname", "target_field": "Customer.LastName",  "confidence": 0.99, "transform_hint": null },
  { "source_column": "dob",   "target_field": "Customer.DateOfBirth","confidence": 0.95, "transform_hint": "parse as MM/DD/YY" },
  { "source_column": "cell",  "target_field": "CustomerPhone.PhoneNumber","confidence": 0.92, "transform_hint": "normalize to E.164" },
  { "source_column": "addr1", "target_field": "CustomerAddress.Line1","confidence": 0.97, "transform_hint": null },
  { "source_column": "zip",   "target_field": "CustomerAddress.PostalCode","confidence": 0.98, "transform_hint": null }
]
```

### 3.4 Tier routing logic

```csharp
public IngestionTier DetermineTier(IngestionJob job, List<ColumnMapping> mappings)
{
    var avgConfidence = mappings.Average(m => m.ConfidenceScore);
    var hasTemplate = job.TemplateId != null;

    if (hasTemplate && avgConfidence >= 0.92m)
        return IngestionTier.Auto;         // fully automated, no human needed

    if (avgConfidence >= 0.75m)
        return IngestionTier.Review;       // business user reviews flagged rows

    return IngestionTier.ETL;             // queue for ETL team
}
```

### 3.5 Melissa address validation

Call Melissa's Address Object API per address in the staging rows. Store the raw
response in `IngestionStagingRow.MelissaResult` and set a flag reason if it fails.

```csharp
public class MelissaValidationService
{
    public async Task<MelissaResult> ValidateAddressAsync(AddressInput address)
    {
        // POST to https://address.melissadata.net/v3/WEB/GlobalAddress/doGlobalAddress
        // with your Melissa license key from config
        // Returns: standardized address + result codes (AS01 = verified, AE = error)
    }

    public bool IsFlagWorthy(MelissaResult result)
        => result.Results.StartsWith("AE") || result.Results == "GE";
}
```

### 3.6 Background processing

Use a .NET hosted service (or Hangfire if you already have it) to process jobs
asynchronously so the upload endpoint returns immediately.

```csharp
public class IngestionProcessorJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Poll IngestionJob table for Status = 'Pending' every 5s
        // Or use a channel/queue pushed to by the upload endpoint
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await _jobRepo.DequeueNextPendingAsync();
            if (job != null)
                await ProcessJobAsync(job, stoppingToken);
            else
                await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(IngestionJob job, CancellationToken ct)
    {
        // 1. Parse file (CSV via CsvHelper, XLSX via ClosedXML)
        // 2. Check schema fingerprint → load template if match
        // 3. Call AiMappingService.InferMappingsAsync()
        // 4. Route tier
        // 5. Normalize all rows → insert into IngestionStagingRow
        // 6. Run Melissa on address rows
        // 7. Score each row → flag rows below threshold
        // 8. Update job Status
        // 9. Push SignalR progress update to UI
    }
}
```

### 3.7 NuGet packages to add

```xml
<PackageReference Include="CsvHelper" Version="33.*" />
<PackageReference Include="ClosedXML" Version="0.102.*" />
<PackageReference Include="Anthropic.SDK" Version="*" />   <!-- or raw HttpClient -->
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="*" />
```

---

## 4. Frontend — React JS

### 4.1 Component structure

```
/src
  /pages
    /ingestion
      IngestionUploadPage.jsx      ← drag-drop upload + job list
      IngestionReviewPage.jsx      ← row-by-row review for business users
      IngestionMappingPage.jsx     ← ETL team column mapping editor
  /components
    /ingestion
      FileDropZone.jsx
      JobStatusBadge.jsx
      StagingRowTable.jsx
      ColumnMappingEditor.jsx
      ConfidenceBar.jsx
  /hooks
    useIngestionJob.js             ← polling or SignalR hook
    useSignalR.js
  /api
    ingestionApi.js                ← axios calls to /api/ingestion
```

### 4.2 Upload flow (IngestionUploadPage)

```jsx
// Key behaviors:
// 1. Drag-drop or click to select .csv or .xlsx only
// 2. POST multipart/form-data to /api/ingestion/upload
// 3. On 202 Accepted → store jobId, begin polling /api/ingestion/{jobId}/status
// 4. Show real-time progress bar via SignalR hub connection
// 5. On status = AwaitingReview → redirect to IngestionReviewPage
// 6. On status = Complete → show summary card (passed/flagged/failed counts)
```

### 4.3 Business user review (IngestionReviewPage)

```jsx
// Displays flagged rows in a paginated table
// Each row shows:
//   - Row number from source file
//   - Field values (normalized)
//   - Flag reasons (e.g. "Address not verified by Melissa", "Confidence < 0.80")
//   - Approve / Reject buttons
// "Approve All" bulk button for power users
// Once all flagged rows are resolved → Enable "Commit" button
// Commit calls POST /api/ingestion/{jobId}/commit
```

### 4.4 SignalR real-time updates

```js
// useSignalR.js
import * as signalR from '@microsoft/signalr';

export function useIngestionProgress(jobId, onUpdate) {
  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/ingestion')
      .withAutomaticReconnect()
      .build();
    conn.on('JobProgress', (update) => { if (update.jobId === jobId) onUpdate(update); });
    conn.start();
    return () => conn.stop();
  }, [jobId]);
}
```

### 4.5 npm packages to add

```bash
npm install @microsoft/signalr axios react-dropzone
```

---

## 5. Azure Infrastructure

### 5.1 Services

| Service | Purpose |
|---|---|
| Azure Blob Storage | Temporary file staging (upload → process → delete) |
| Azure Service Bus (optional) | Replace in-process queue for scale |
| Azure App Service | API hosting |
| Azure Static Web Apps | React UI hosting |
| Azure SQL | SQL Server managed |
| Azure Key Vault | Melissa API key, Anthropic API key, connection strings |
| Application Insights | Logging, ingestion job telemetry |

### 5.2 File handling

Upload the file to a Blob Storage container (`ingestion-staging`) rather than
holding it in memory. Store the blob URL in `IngestionJob`. Delete the blob after
the job reaches `Complete` or `Failed`. This keeps the API stateless and supports
multiple app service instances.

### 5.3 Azure DevOps pipeline notes

The CI/CD pipeline to dev should:
1. Run `dotnet build` and `dotnet test`
2. Run `npm run build` in the UI project
3. Deploy the `.sqlproj` via `SqlPackage.exe` publish (dacpac) — this is idempotent and handles schema drift per environment
4. Deploy API to App Service, UI to Static Web Apps

---

## 6. Pipeline Processing Summary

```
File arrives
    │
    ▼
[IngestionJob created — Status: Pending]
    │
    ▼
Background processor picks up job
    │
    ├─ Parse file (CsvHelper / ClosedXML)
    ├─ Compute schema fingerprint
    ├─ Load prior mapping template if fingerprint matches
    │
    ▼
[AiMappingService → Claude API]
    │
    ├─ Returns column mappings + confidence scores
    ├─ Determine tier (Auto / Review / ETL)
    │
    ▼
[DataNormalizationService]
    │
    ├─ Apply transform hints from mapping
    ├─ Normalize phones → E.164
    ├─ Normalize dates → ISO 8601
    ├─ Normalize emails → lowercase
    │
    ▼
[MelissaValidationService — address rows only]
    │
    ├─ CASS certify each address
    ├─ Flag rows with AE result codes
    │
    ▼
[Score each row → insert into IngestionStagingRow]
    │
    ├─ Tier = Auto + all rows Pass → skip to Commit
    ├─ Tier = Review → Status: AwaitingReview → business user UI
    ├─ Tier = ETL → Status: AwaitingReview → ETL team queue
    │
    ▼
[Human review if needed]
    │
    ▼
[IngestionCommitService]
    │
    ├─ Upsert into Customer, CustomerEmail, CustomerPhone, CustomerAddress
    ├─ Insert into CustomerAttribute (per-org key/value)
    ├─ Insert into CustomerModelFeature (LLM signals)
    ├─ Save approved mapping as MappingTemplate for next time
    │
    ▼
[IngestionJob Status: Complete]
```

---

## 7. How to Hand This to Claude Code

1. Drop this file at the root of your monorepo as `INGESTION_PIPELINE_SPEC.md`
2. Open Claude Code in VS Code (`claude` in terminal at project root)
3. Start with: *"Read INGESTION_PIPELINE_SPEC.md. I want to implement the ingestion
   pipeline. Start with the SQL Server migration scripts for the new tables, then
   the backend services. Ask me before creating any files you're unsure about."*
4. Claude Code will read your existing project structure and fit the new code into
   your conventions — controllers, service patterns, EF Core setup, etc.

### Suggested implementation order

1. DB migration scripts (SQL Server DB project)
2. EF Core models + DbContext additions
3. `IngestionJobService` + `IngestionController` (upload + status endpoints)
4. File parser utility (CSV + XLSX)
5. `AiMappingService` (Claude API call + response parsing)
6. `DataNormalizationService`
7. `MelissaValidationService`
8. Background processor / hosted service
9. SignalR hub
10. React upload page + job status polling
11. React review page (flagged rows)
12. Azure Blob Storage integration for file staging

---

## 8. Open Questions to Resolve

- Does your existing `Customer` table use `UNIQUEIDENTIFIER` or `INT` PKs?
- Do you already have an auth/claims setup that exposes `OrganizationId`? 
  (The spec assumes `User.GetOrganizationId()` extension method exists)
- Is Hangfire already in the project, or should the background job use a plain
  .NET `IHostedService`?
- Do you have a Melissa license already? Which product tier (Address Object, 
  Global Address Verification)?
- What is the Anthropic API key management story — Key Vault already wired up?
- Should the ETL team queue be a separate UI view, or an email notification?
