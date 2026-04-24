\# CLAUDE.md — Customer Validation Portal



This file provides context for Claude Code sessions working on the

POC Customer Validation project. Read this before making any changes.



\---



\## Project overview



A multi-tenant platform that allows organisations to collect and validate

customer data through configurable field definitions. Admins configure

what fields each organization needs. Customers review and confirm their

pre-populated data through a validation portal.



\---



\## Solution structure



```

EchoCustomerValidation/

├── POC.CustomerValidation/

│   └── POC.CustomerValidation.API/          # .NET 10 Web API

│       ├── Controllers/

│       ├── Interfaces/

│       ├── Middleware/

│       ├── Models/

│       │   ├── DTOs/

│       │   └── Entites/

│       ├── Persistence/

│       │   └── Repositories/

│       ├── Services/

│       └── Startup/

│           └── SerilogSetup.cs

├── ClientAdmin/

│   └── datavalidation-portal/               # Admin React SPA (Vite + JS)

│       └── src/

│           ├── api/

│           │   ├── organization.js                # fetch-based HTTP organization

│           │   └── services.js              # all API call functions

│           ├── components/

│           │   ├── common/index.jsx         # Spinner, Toast, EmptyState, etc.

│           │   ├── fields/

│           │   │   └── FieldOptionsModal.jsx

│           │   └── layout/

│           │       └── AppLayout.jsx        # sidebar + topbar layout

│           ├── hooks/

│           │   └── useApi.js                # React Query hooks

│           ├── pages/

│           │   ├── organizationsPage.jsx

│           │   └── FieldDefinitionsPage.jsx

│           └── styles/

│               └── main.scss               # Bootstrap overrides + layout

└── docs/

&#x20;   └── AZURE\_AD\_SETUP.md

```



\---



\## Tech stack



\### Backend — .NET 10 API

\- \*\*Framework:\*\* ASP.NET Core .NET 10, controller-based (not minimal API)

\- \*\*Data access:\*\* Dapper (SQL-first, no EF Core)

\- \*\*Database:\*\* SQL Server with `NEWID()` GUIDs as primary keys

\- \*\*Logging:\*\* Serilog → two SQL Server tables (InformationLogs, ErrorLogs) split by log level

\- \*\*API docs:\*\* Scalar UI via `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore`

\- \*\*Validation:\*\* FluentValidation

\- \*\*Auth:\*\* Azure AD (not yet wired — placeholder for future)

\- \*\*Connection factory:\*\* `IDbConnectionFactory` / `SqlConnectionFactory` registered as Singleton



\### Frontend — Admin SPA

\- \*\*Framework:\*\* React 18, Vite, plain JavaScript (NO TypeScript)

\- \*\*Styling:\*\* Bootstrap 5 + SASS (DM Sans font)

\- \*\*HTTP:\*\* Native `fetch` (no axios — removed due to issues)

\- \*\*State:\*\* React Query (`@tanstack/react-query`) for all server state

\- \*\*Forms:\*\* `react-hook-form`

\- \*\*Drag/drop:\*\* `@dnd-kit/core`, `@dnd-kit/sortable`

\- \*\*Auth:\*\* None currently (Azure AD MSAL removed for POC phase)

\- \*\*Routing:\*\* React Router v6



\---



\## Database schema (SQL Server)



\### Core tables



```

Organizations           — tenant/business table (Id, Name, OrganizationCode, ...)

Customers               — people whose data is collected (Id, OrganizationId, FirstName, LastName, CustomerCode, ...)

&#x20;                         Uses SQL Server temporal tables (SYSTEM\_VERSIONING)

FieldSections           — optional grouping of fields (Id, OrganizationId, SectionName, DisplayOrder)

FieldDefinitions        — field config per org (Id, OrganizationId, FieldSectionId, FieldKey, FieldLabel,

&#x20;                         FieldType, IsRequired, IsActive, DisplayOrder, MinValue, MaxValue, ...)

FieldOptions            — dropdown/multiselect choices (Id, FieldDefinitionId, OptionKey, OptionLabel, DisplayOrder)

FieldValues             — customer data (Id, CustomerId, FieldDefinitionId, ValueText, ValueNumber,

&#x20;                         ValueDate, ValueDatetime, ValueBoolean, ConfirmedAt, FlaggedAt, FlagNote, ...)

```



\### Field types supported

`text` | `number` | `date` | `datetime` | `checkbox` | `dropdown` | `multiselect`



\### Value storage pattern

One row in FieldValues per customer per field. Only the column matching

the FieldType is populated — the rest are NULL. OptionKey values (e.g. `bach`,

`CA`) are stored in ValueText for dropdown fields.



\### Audit / history

A trigger on FieldValues writes the old row to FieldValue\_History before

every UPDATE. History includes ChangedBy, ChangedAt, ChangeReason.



\---



\## Coding conventions



\### C# / .NET

\- Use primary constructors for dependency injection:

&#x20; ```csharp

&#x20; public class OrganizationService(IOrganizationRepository repo, ILogger<OrganizationService> log)

&#x20; ```

\- Repositories use Dapper with raw SQL in verbatim string literals (`""" ... """`)

\- SQL column aliases match entity property names exactly (e.g. `Id as OrganizationId`)

\- Services map entities to DTOs via private static `Map()` methods

\- DTOs use non-positional records with `init` properties (not positional constructors)

&#x20; so object initializer syntax works in Map methods

\- All services accept `ILogger<T>` (generic) — never plain `ILogger`

\- Middleware uses convention-based pattern (NOT `IMiddleware` interface)

&#x20; — do NOT register middleware with `AddTransient`, just use `app.UseMiddleware<T>()`

\- Repository methods return entities; services return DTOs

\- `IDbConnectionFactory` is registered as Singleton; repositories are Scoped

\- All DI registrations must be before `builder.Build()` — never against `app.Services`



\### React / JavaScript

\- Plain JavaScript only — NO TypeScript, no `.ts` or `.tsx` files

\- All files use `.js` or `.jsx` extensions

\- `fetch`-based API organization in `src/api/organization.js` wraps responses as `{ data }`

&#x20; so services can call `.then(r => r.data)` consistently

\- React Query hooks in `src/hooks/useApi.js` — query keys defined in `QK` object

\- Components use named exports except pages which use default exports

\- Forms use `react-hook-form` — numeric fields use empty string defaults,

&#x20; converted to null in onSubmit

\- No authentication currently — MSAL removed



\### SQL scripts

\- Always wrap seed/migration scripts in `BEGIN TRANSACTION` / `BEGIN TRY` / `BEGIN CATCH`

\- Guard against duplicates before inserting (check if data already exists)

\- `RAISERROR` in catch block to re-raise after rollback

\- Use `SET NOCOUNT ON` at the top of all scripts



\---



\## Existing API endpoints



\### Organisations

```

GET    /api/organisations                    	List all

GET    /api/organisations/{id}               	Get by ID

POST   /api/organisations                    	Create

PUT    /api/organisations/{id}               	Update

PATCH  /api/organisations/{id}/status{status}	Change active status

```



\### Field Definitions

```

GET    /api/fields        					List fields for organization

GET    /api/fields/{fieldId}   					Get single field

POST   /api/fields        					Create field

PUT    /api/fields/{fieldId}   					Update field

PATCH  /api/fields/{fieldId}/status/{status}			Deactivate field

PATCH  /api/fields/organization/{organizationId}/reorder	Reorder fields

```



\### Field Options

```

GET    /api/fields/{fieldId}/options         List options

POST   /api/fields/{fieldId}/options         Add option

PUT    /api/fields/{fieldId}/options/{id}    Update option

DELETE /api/fields/{fieldId}/options/{id}    Delete option

PUT    /api/fields/{fieldId}/options/bulk    Replace all options

```



\### Customer Field Values (admin read-only)

```

GET    /api/customers/{customerId}/values              All values for customer

GET    /api/customers/{customerId}/values/history      Change history (paginated)

GET    /api/customers/{customerId}/values/{fieldId}/history  History for one field

```



\### DTOs API expects



namespace POC.CustomerValidation.API.Models.DTOs;



// -------------------------------------------------------

// Organization DTOs

// -------------------------------------------------------

public record OrganizationDto(

&#x20;   Guid        OrganizationId,

&#x20;   string      OrganizationName,

&#x20;   string      OrganizationCode,

&#x20;   string?     FilingName, 

&#x20;   string?     MarketingName,

&#x20;   string?     Abbreviation,

&#x20;   string?     Website,

&#x20;   string?     Phone,

&#x20;   string?     CompanyEmail,

&#x20;   bool        IsActive,

&#x20;   DateTime    CreatedAt,

&#x20;   string      CreatedBy,

&#x20;   DateTime    UpdatedAt,

&#x20;   string?     ModifiedBy

);



public record CreateOrganizationRequest(

&#x20;   Guid    OrganizationId,

&#x20;   string  OrganizationName,

&#x20;   string  OrganizationCode,

&#x20;   string? FilingName,

&#x20;   string? MarketingName,

&#x20;   string? Abbreviation,

&#x20;   string? Website,

&#x20;   string? Phone,

&#x20;   string? CompanyEmail

);



public record UpdateOrganizationRequest(

&#x20;   Guid OrganizationId,

&#x20;   string  OrganizationName,

&#x20;   string  OrganizationCode,

&#x20;   string? FilingName,

&#x20;   string? MarketingName,

&#x20;   string? Abbreviation,

&#x20;   string? Website,

&#x20;   string? Phone,

&#x20;   string? CompanyEmail,

&#x20;   bool?   IsActive

);







// -------------------------------------------------------

// Field Sections

// -------------------------------------------------------



public record FieldSectionDto(

&#x20;   Guid    SectionId,

&#x20;   Guid    OrganizationId,

&#x20;   string  SectionName,

&#x20;   int     DisplayOrder,

&#x20;   bool    IsActive

);



public record CreateFieldSectionRequest(

&#x20;   Guid    OrganizationId,

&#x20;   string  SectionName,

&#x20;   int     DisplayOrder = 0

);



public record UpdateFieldSectionRequest(

&#x20;   string  SectionName,

&#x20;   int     DisplayOrder,

&#x20;   bool    IsActive

);





public record ReorderFieldRequest(Guid FieldDefinitionId, int DisplayOrder);



// -------------------------------------------------------

// Field Definitions

// -------------------------------------------------------



public record FieldDefinitionDto(

&#x20;   Guid        FieldDefinitionId,

&#x20;   Guid        OrganizationId,

&#x20;   Guid?       SectionId,

&#x20;   string?     SectionName,

&#x20;   string      FieldKey,

&#x20;   string      FieldLabel,

&#x20;   string      FieldType,

&#x20;   string?     PlaceholderText,

&#x20;   string?     HelpText,

&#x20;   bool        IsRequired,

&#x20;   bool        IsActive,

&#x20;   int         DisplayOrder, 

&#x20;   decimal?    MinValue,

&#x20;   decimal?    MaxValue,

&#x20;   int?        MinLength,

&#x20;   int?        MaxLength,

&#x20;   string?     RegexPattern,

&#x20;   IEnumerable<FieldOptionDto> Options

);







public record CreateFieldDefinitionRequest(

&#x20;   Guid        OrganizationId,

&#x20;   Guid?       SectionId,

&#x20;   string      FieldKey,

&#x20;   string      FieldLabel,

&#x20;   string      FieldType,

&#x20;   string?     PlaceholderText,

&#x20;   string?     HelpText,

&#x20;   bool        IsRequired      = false,

&#x20;   int         DisplayOrder    = 0,

&#x20;   decimal?    MinValue        = null,

&#x20;   decimal?    MaxValue        = null,

&#x20;   int?        MinLength       = null,

&#x20;   int?        MaxLength       = null,

&#x20;   string?     RegexPattern    = null

);





public record UpdateFieldDefinitionRequest(

&#x20;   Guid        FieldDefinitionId,

&#x20;   Guid?       SectionId,

&#x20;   string      FieldLabel,

&#x20;   string      FieldType,

&#x20;   string?     PlaceholderText,

&#x20;   string?     HelpText,

&#x20;   bool        IsRequired,

&#x20;   bool        IsActive,

&#x20;   int         DisplayOrder,

&#x20;   decimal?    MinValue,

&#x20;   decimal?    MaxValue,

&#x20;   int?        MinLength,

&#x20;   int?        MaxLength,

&#x20;   string?     RegexPattern

);









// -------------------------------------------------------

// Field Options

// -------------------------------------------------------



public record FieldOptionDto(

&#x20;   Guid        OptionId,

&#x20;   Guid        FieldDefinitionId,

&#x20;   string      OptionKey,

&#x20;   string      OptionLabel,

&#x20;   int         DisplayOrder,

&#x20;   bool        IsActive

);





public record CreateFieldOptionRequest(

&#x20;   string  OptionKey,

&#x20;   string  OptionLabel,

&#x20;   int     DisplayOrder = 0

);



public record UpdateFieldOptionRequest(

&#x20;   string  OptionKey,

&#x20;   string  OptionLabel,

&#x20;   int     DisplayOrder,

&#x20;   bool    IsActive

);



public record BulkUpsertFieldOptionsRequest(

&#x20;   IEnumerable<CreateFieldOptionRequest> Options

);







// -------------------------------------------------------

// Field Values

// -------------------------------------------------------

public record FieldValueDto(

&#x20;   Guid FieldValueId,

&#x20;   Guid CustomerId,

&#x20;   Guid FieldDefinitionId,

&#x20;   string FieldLabel,

&#x20;   string FieldType,

&#x20;   string? ValueText,

&#x20;   decimal? ValueNumber,

&#x20;   DateOnly? ValueDate,

&#x20;   DateTime? ValueDatetime,

&#x20;   bool? ValueBoolean,

&#x20;   string? DisplayValue,

&#x20;   DateTime? ConfirmedAt,

&#x20;   string? ConfirmedBy,

&#x20;   DateTime? FlaggedAt,

&#x20;   string? FlagNote,

&#x20;   DateTime CreatedDt,

&#x20;   DateTime ModifiedDt

);





public record FieldValueHistoryDto(

&#x20;   Guid HistoryId,

&#x20;   Guid FieldValueId,

&#x20;   Guid FieldDefinitionId,

&#x20;   Guid CustomerId,

&#x20;   string FieldLabel,

&#x20;   string? OldValue,

&#x20;   string? ChangedBy,

&#x20;   DateTime ChangedAt,

&#x20;   string? ChangeReason

);





// -------------------------------------------------------

// Shared DTOs

// -------------------------------------------------------

public record PagedResult<T>(

&#x20;   IEnumerable<T> Items,

&#x20;   int TotalCount,

&#x20;   int Page,

&#x20;   int PageSize

);



public record ApiError(

&#x20;   string Code,

&#x20;   string Message,

&#x20;   Dictionary<string, string\[]>? ValidationErrors = null

);



















\---



\## What still needs to be built



\### Customer validation portal (NOT started)

A separate React app (`/client-portal`) for customers to review and confirm

their data. No authentication for now (same as admin).



\*\*New API endpoints needed:\*\*

```

GET  /api/portal/customers/{identifier}         Load customer + field values

PUT  /api/portal/values/{valueId}/confirm       Confirm a field value

PUT  /api/portal/values/{valueId}/flag          Flag a field with a note

POST /api/portal/sessions                       Start a validation session

PUT  /api/portal/sessions/{sessionId}/complete  Complete the session

```



\*\*Portal React app pages:\*\*

1\. `CustomerLookup` — customer enters email or customer code

2\. `ValidationForm` — dynamic form driven by FieldDefinitions from DB,

&#x20;  renders correct widget per FieldType, Confirm/Flag per field, progress bar

3\. `Complete` — summary of confirmed vs flagged counts



\### Admin — Customers page (NOT started)

The Customers nav item exists in the sidebar but has no page yet.

Needs: customer list per org, search, validation progress bar per customer,

flagged field count, link to view individual customer field values.



\---



\## Serilog setup



Two SQL tables split by log level:

\- `InformationLogs` — Information level only

\- `ErrorLogs` — Error and Fatal only



Custom SQL columns on both tables: `Application`, `RequestPath`, `RequestBody`, `CorrelationId`



Application name read from `appsettings.json`:

```json

{

&#x20; "ApplicationName": "POC.CustomerValidation.API"

}

```



Enrichers: `FromLogContext`, `WithCorrelationId`, `WithMachineName`, `WithEnvironmentName`, `WithThreadId`



\---



\## Running the project



\### Both together (from the admin React project folder)

```bash

npm run dev:all

```



This uses `concurrently` to start both the .NET API and the React dev server.



\### API only

```bash

dotnet run --project ../../POC.CustomerValidation/POC.CustomerValidation.API --launch-profile https

```

API available at: `https://localhost:7017`

Scalar UI at: `https://localhost:7017/scalar/v1`



\### Admin React only

```bash

cd ClientAdmin/datavalidation-portal

npm run dev

```

Available at: `http://localhost:5173`



\### Environment variables (admin React — `.env.local`)

```

VITE\_AAD\_CLIENT\_ID=       (not currently used — auth disabled)

VITE\_AAD\_TENANT\_ID=       (not currently used — auth disabled)

VITE\_AAD\_API\_CLIENT\_ID=   (not currently used — auth disabled)

```



\---



\## Key design decisions



1\. \*\*Schema-free field storage\*\* — field definitions are rows in a table, not

&#x20;  database columns. Adding a new field for a client requires no schema migration.



2\. \*\*Typed value columns\*\* — FieldValues has separate columns per type

&#x20;  (ValueText, ValueNumber, ValueDate, etc.) rather than a single NVARCHAR,

&#x20;  keeping values queryable without casting.



3\. \*\*Dapper over EF Core\*\* — SQL-first for full control over queries,

&#x20;  especially important for the dynamic field value patterns.



4\. \*\*No axios\*\* — removed due to compatibility issues; replaced with a thin

&#x20;  native fetch wrapper in `src/api/client.js` that mirrors the axios interface

&#x20;  so service call signatures remain unchanged.



5\. \*\*Convention-based middleware\*\* — middleware classes use `InvokeAsync(HttpContext)`

&#x20;  pattern, NOT the `IMiddleware` interface. Do not register them in DI with AddTransient.



6\. \*\*Non-positional DTO records\*\* — DTOs use `{ get; init; }` properties so Map()

&#x20;  methods can use named object initializer syntax, which is safer with many fields

&#x20;  than positional constructor argument order.

