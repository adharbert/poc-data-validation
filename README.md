# Customer Validation Portal — POC

A multi-tenant platform for collecting and validating customer data through
configurable, schema-free field definitions. Internal staff configure fields and
import data; customers review their pre-populated records and confirm or flag each field.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Tech Stack](#tech-stack)
4. [Build Status](#build-status)
5. [Solution Structure](#solution-structure)
6. [Getting Started](#getting-started)
7. [Database Setup](#database-setup)
8. [Configuration](#configuration)
9. [Key Design Decisions](#key-design-decisions)
10. [Documentation Reference](#documentation-reference)

---

## Overview

The platform has two sides:

| Portal | Who uses it | Status |
|---|---|---|
| **Admin Portal** (port 5173) | Internal staff, ETL teams | In process |
| **Customer Validation Portal** (port 5174) | End customers | Not yet built |

**Core concept:** Field definitions are stored as rows in a database table — not as
columns. Adding a new data field for a client requires zero schema migrations.
A new row in `FieldDefinitions` is all it takes.

---

## Architecture

```
┌────────────────────────────────────────────────────┐
│              React Admin SPA                       │
│      Vite · Bootstrap 5 · React Query              │
│          http://localhost:5173                     │
└───────────────────┬────────────────────────────────┘
                    │  fetch /api/*
┌───────────────────▼────────────────────────────────┐
│             .NET 10 Web API                        │
│       Dapper · SQL Server · Serilog                │
│          https://localhost:7124                    │
│          Scalar UI: /scalar/v1                     │
└───────────────────┬────────────────────────────────┘
                    │
┌───────────────────▼────────────────────────────────┐
│               SQL Server                           │
│     Temporal tables · Triggers · Soft-delete       │
└────────────────────────────────────────────────────┘

  React Customer Portal (planned — port 5174, same API)
```

---

## Tech Stack

### Backend

| Concern | Technology |
|---|---|
| Framework | .NET 10, ASP.NET Core (controller-based) |
| Data access | Dapper — SQL-first, no EF Core |
| Database | SQL Server |
| Logging | Serilog → Console (always) + SQL Server sinks (config-toggled) |
| API docs | Scalar UI via `Microsoft.AspNetCore.OpenApi` |
| CSV parsing | CsvHelper 33.x |
| Excel parsing | ClosedXML 0.104.x |
| Authentication | Azure AD via `Microsoft.Identity.Web` (stubbed — not active in POC) |

### Frontend — Admin SPA

| Concern | Technology |
|---|---|
| Framework | React 18, Vite 6 |
| NPM | 24.11.0 or newer |
| Language | Plain JavaScript — **no TypeScript** |
| Styling | Bootstrap 5.3 + SASS (compiled by Live SASS Compiler extension) |
| Font | DM Sans (Google Fonts) |
| HTTP | Native `fetch` wrapper — **no axios** |
| Server state | `@tanstack/react-query` v5 |
| Forms | `react-hook-form` v7 |
| Drag and drop | `@dnd-kit/core` + `@dnd-kit/sortable` |
| Routing | React Router v6 |
| Authentication | None — MSAL removed for POC phase |

---

## Build Status

### Completed

| Feature | Admin SPA route | Notes |
|---|---|---|
| Dashboard | `/dashboard` | Global stat cards, org comparison chart, expiring projects |
| Organizations CRUD | `/organizations` | Create, edit, activate/deactivate |
| Organisation Detail | `/organizations/:id` | Per-org stats, contracts, projects timeline, nav tiles |
| Field Sections | `/organizations/:id/inputs` | Part of Inputs page |
| Field Definitions (Inputs) | `/organizations/:id/inputs` | CRUD, type-aware (incl. phone), options, drag-and-drop |
| Form Preview | `/organizations/:id/inputs` | Admin selects customer, sees live form |
| Customers | `/organizations/:id/customers` | Paginated list, create/edit/activate/deactivate |
| Contracts | (within org context) | Per-org, single active constraint |
| Marketing Projects | (within org context) | Per-org, multiple active allowed |
| CSV/Excel Import | `/organizations/:id/import` | 5-step wizard: upload → map → preview → execute |
| Import Staging | `/organizations/:id/import-staging` | Resolve unmatched columns |
| Organisation search | `/organizations` | Server-side `?search=` on name/abbreviation/code; debounced input |
| Dashboard org filter | `/dashboard` | Client-side search filter on org comparison table |
| Phone field type | `/organizations/:id/inputs` | Stored as digits; live `(XXX) XXX-XXXX` masking on input |
| Breadcrumb navigation | all sub-pages | Organisations → Org name → Current page |
| Consistent date formatting | — | All dates via `fmtDate()` — MM/dd/yyyy |
| Number display formatting | — | `fmtNumber()` strips trailing zeros — `42.00` → `42`, `3.14` → `3.14` |
| Serilog config | — | DB sinks toggled per environment via appsettings |
| Unit test suite | — | `POC.CustomerValidation.Test` — 142 xUnit tests, all 10 controllers, ≥ 90% coverage |
| Customer Addresses | — | `CustomerAddresses` table (temporal), address history, Melissa validation stub, confirm endpoint |

### Remaining

| Feature | Notes |
|---|---|
| Customer detail page | Drill-down from customers list — API already exists |
| Address UI | Admin form for creating/editing customer addresses — backend ready |
| Melissa integration | `MelissaService` is a stub — wire up real REST API when credentials available |
| Customer Validation Portal | Separate Vite app in `ClientPortal/` — not yet scaffolded |
| Authentication | Azure AD (admin) + magic-link (customer portal) |

---

## Solution Structure

```
poc-data-validation/
├── README.md                                   ← This file
├── CLAUDE.md                                   ← Claude Code session context
│
├── POC.CustomerValidation/
│   ├── POC.CustomerValidation.slnx             ← .NET 10 solution (new slnx format)
│   ├── POC.CustomerValidation.API/             ← .NET 10 Web API
│   │   ├── Controllers/
│   │   │   ├── OrganizationsController.cs
│   │   │   ├── FieldDefinitionsController.cs
│   │   │   ├── FieldSectionsController.cs      ← sections + form preview
│   │   │   ├── CustomersController.cs
│   │   │   ├── ContractsController.cs
│   │   │   ├── MarketingProjectsController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── ImportController.cs
│   │   │   └── ImportStagingController.cs
│   │   ├── Interfaces/
│   │   │   ├── IRepositories.cs
│   │   │   └── IServices.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Models/
│   │   │   ├── DTOs/DTOs.cs
│   │   │   └── Entites/                        ← DB entity classes
│   │   ├── Persistence/
│   │   │   └── Repositories/
│   │   ├── Services/
│   │   ├── Startup/
│   │   │   ├── DependencyInjectionSetup.cs
│   │   │   └── SerilogSetup.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Production.json
│   │   └── Program.cs
│   │
│   ├── POC.CustomerValidation.Test/            ← xUnit test project (.NET 10)
│   │   └── Controllers/                        ← One test class per controller (142 tests)
│   │
│   └── POC.Database/                           ← SSDT database project
│       ├── Tables/
│       ├── scripts/Post-Deployment/            ← Seed + migration scripts
│       └── POC.Database.publish.xml
│
├── ClientAdmin/
│   └── datavalidation-portal/                  ← Admin React SPA
│       ├── public/
│       │   └── pci-logo.svg
│       ├── src/
│       │   ├── api/
│       │   │   ├── client.js                   ← fetch wrapper
│       │   │   └── services.js                 ← all API call functions
│       │   ├── assets/
│       │   │   ├── scss/main.scss              ← SCSS source (edit this)
│       │   │   └── css/                        ← compiled output (do not edit)
│       │   ├── components/
│       │   │   ├── common/index.jsx            ← shared components
│       │   │   └── layout/AppLayout.jsx        ← sidebar + topbar shell
│       │   ├── hooks/useApi.js                 ← all React Query hooks
│       │   ├── pages/
│       │   │   ├── DashboardPage.jsx
│       │   │   ├── OrganizationsPage.jsx
│       │   │   ├── OrgDetailPage.jsx           ← org landing: stats, contracts, projects
│       │   │   ├── CustomersPage.jsx
│       │   │   ├── InputsPage.jsx              ← sections + fields + preview
│       │   │   ├── ImportPage.jsx
│       │   │   └── ImportStagingPage.jsx
│       │   ├── utils/
│       │   │   └── dates.js                    ← fmtDate (MM/dd/yyyy) + fmtPhone
│       │   ├── App.jsx
│       │   └── main.jsx
│       ├── package.json
│       └── vite.config.js
│
├── ClientPortal/                               ← Customer portal (not yet built)
│
└── TASK-FILES/                                 ← All project documentation
    ├── README.md
    ├── TASK-01-project-structure.md
    ├── TASK-02-PROJECT_PLAN.md
    ├── TASK-03-CODING_CONVENTIONS.md
    ├── TASK-04-DATABASE.md
    ├── TASK-05-API.md
    ├── TASK-06-FRONTEND.md
    ├── TASK-07-IMPORT.md
    ├── TASK-08-WHATS_NEXT.md
    └── TASK-09-CONTRACTS-PROJECTS.md
```

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- SQL Server (local or Azure SQL)
- VS Code with **Live SASS Compiler** extension (Glenn2223 fork) for SCSS changes

### Run both API and React together

```bash
cd ClientAdmin/datavalidation-portal
npm install
npm run dev:all
```

This starts both the .NET API and the Vite dev server via `concurrently`.

### Run API only

```bash
dotnet run --project POC.CustomerValidation/POC.CustomerValidation.API --launch-profile https
```

- API: `https://localhost:7124`
- Scalar interactive docs: `https://localhost:7124/scalar/v1`
- OpenAPI JSON: `https://localhost:7124/openapi/v1.json`
- Health check: `https://localhost:7124/health`

### Run unit tests

```bash
cd POC.CustomerValidation
dotnet test POC.CustomerValidation.Test/POC.CustomerValidation.Test.csproj
```

142 tests across all 10 controllers. All tests must pass before merging.

### Run admin React only

```bash
cd ClientAdmin/datavalidation-portal
npm run dev
```

- App: `http://localhost:5173`
- The Vite dev server proxies `/api/*` to the API — no CORS configuration needed.

### SCSS / Styling

The SCSS source is `ClientAdmin/datavalidation-portal/src/assets/scss/main.scss`.
The **Live SASS Compiler** VS Code extension compiles it automatically on save to:
- `src/assets/css/main.css` (expanded)
- `src/assets/css/main.min.css` (compressed)

Vite imports the compiled CSS — it does not process SCSS itself.
**Do not edit the CSS files directly.**

---

## Database Setup

The database is managed via an SSDT project (`POC.Database/`). Publish via Visual Studio
using the profile `POC.Database.publish.xml` (data-loss protection is disabled in the
profile to allow iterative schema changes).

### Post-deployment scripts — run in this order

| Order | Script | Purpose |
|---|---|---|
| 1 | *(base schema)* | Core tables created by DACPAC |
| 2 | `01_SeedData.sql` | 50 test customers |
| 3 | `02_SeedDataFieldOptions_States.sql` | US states dropdown options |
| 4 | `03_SeedDataFieldOptions_HighestDegree.sql` | Degree level options |
| 5 | `04_Migration_ImportTables.sql` | Import + staging tables |
| 6 | `05_Contract_3CFDCADA.sql` | Seed contract for ADX org |
| 7 | `06_MarketingProject_ADX.sql` | Seed marketing project for ADX org |
| 8 | `Migration_003_FieldDefinitions_Phone.sql` | Adds `DisplayFormat` column to `FieldDefinitions` |

Scripts are idempotent — safe to re-run. Each checks for existing data before inserting.

### Key schema notes

- Every table is scoped to `OrganizationId`
- `Customers` uses SQL Server **temporal tables** (full row history built-in)
- `FieldValues` stores typed values in separate columns (`ValueText`, `ValueNumber`,
  `ValueDate`, `ValueBoolean`) — no EAV string casting

### Table relationships

```
Organizations
    ├── FieldSections          (OrganizationId FK)
    │       └── FieldDefinitions (FieldSectionId FK, nullable)
    │               ├── FieldOptions
    │               └── FieldValues ◄───────────────────┐
    ├── Customers                                       │
    │       └── FieldValues (CustomerId + FieldId FK)───┘
    ├── Contracts
    ├── MarketingProjects
    └── ImportBatches
            ├── ImportColumnMappings → FieldDefinitions
            ├── ImportValueMappings
            ├── ImportErrors
            └── SavedColumnMappings
```

---

## Configuration

### `appsettings.json` (base — development defaults)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "Sinks": {
      "InformationDb": { "Enabled": false },
      "ErrorDb":       { "Enabled": true  }
    }
  },
  "DashboardSettings": {
    "WarningDaysThreshold": 30
  }
}
```

### Serilog sinks

| Sink | Always on? | Controlled by |
|---|---|---|
| Console | Yes | Always active |
| `InformationLogs` table | No | `Serilog:Sinks:InformationDb:Enabled` |
| `ErrorLogs` table | No | `Serilog:Sinks:ErrorDb:Enabled` |

`appsettings.Production.json` enables `ErrorDb`, disables `InformationDb`,
and sets minimum level to `Warning`.

---

## Key Design Decisions

### No TypeScript in the frontend
Plain JavaScript only. No `.ts` or `.tsx` files.

### No axios
The `src/api/client.js` fetch wrapper mirrors the axios call shape
(`.then(r => r.data)`) so `services.js` is consistent throughout.

### Repositories return entities; services return DTOs
Mapping between entity and DTO is done via a private static `Map()` method
in each service. Never map in the repository or controller.

### Soft-delete everywhere
Nothing is physically deleted. `IsActive = 0` is used for all deactivation.
Data is retained for reporting and audit purposes.

### `IDbConnectionFactory` → Singleton; repositories → Scoped
The connection factory is a singleton; repositories and services are always Scoped.

### Primary constructors for DI
```csharp
public class MyService(IMyRepository repo, ILogger<MyService> log) { }
```
Never use the old constructor-injection style.


### Phone field type
`FieldDefinitions.FieldType` supports `'phone'` as a valid value. The import pipeline
strips all non-digit characters before storing (`Regex.Replace(value, @"\D", "")`).
The `DisplayFormat` column controls how the stored digits are rendered:
`(XXX) XXX-XXXX`, `XXX-XXX-XXXX`, or `XXX.XXX.XXXX`.

### Dapper `DateOnly` type handler
Dapper has no built-in support for C# `DateOnly`. A `SqlMapper.TypeHandler<DateOnly>`
is registered at startup in `DependencyInjectionSetup.cs`. Never remove it — without it,
any `DateOnly` property on a Dapper-mapped entity will throw `InvalidCastException` at runtime.

### Date formatting — frontend
All date displays in the admin SPA use `fmtDate()` from `src/utils/dates.js`, which always
outputs `MM/dd/yyyy`. Never use raw `toLocaleDateString()` in page components.

---

## Documentation Reference

All detailed documentation lives in [`TASK-FILES/`](TASK-FILES/).

| Document | What's in it |
|---|---|
| [TASK-01 — Project Structure](TASK-FILES/TASK-01-project-structure.md) | Directory layout and file responsibilities |
| [TASK-02 — Project Plan](TASK-FILES/TASK-02-PROJECT_PLAN.md) | Phased build plan — what's done, what's next |
| [TASK-03 — Coding Conventions](TASK-FILES/TASK-03-CODING_CONVENTIONS.md) | All rules — must read before writing any code (includes unit testing patterns) |
| [TASK-04 — Database](TASK-FILES/TASK-04-DATABASE.md) | Full schema: all tables, columns, constraints |
| [TASK-05 — API](TASK-FILES/TASK-05-API.md) | All endpoints with request/response shapes |
| [TASK-06 — Frontend](TASK-FILES/TASK-06-FRONTEND.md) | React app structure, component guide, API client pattern |
| [TASK-07 — Import](TASK-FILES/TASK-07-IMPORT.md) | CSV/Excel import design, 5-step flow, value mapping |
| [TASK-08 — What's Next](TASK-FILES/TASK-08-WHATS_NEXT.md) | Remaining work, prioritised |
| [TASK-09 — Contracts & Projects](TASK-FILES/TASK-09-CONTRACTS-PROJECTS.md) | Contracts, Marketing Projects, Import Staging — business rules |
