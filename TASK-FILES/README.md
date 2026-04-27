# Customer Validation Portal — Project Documentation

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Technology Stack](#technology-stack)
4. [Solution Structure](#solution-structure)
5. [Getting Started](#getting-started)
6. [Documents Index](#documents-index)

---

## Project Overview

A multi-tenant platform that allows organisations to collect and validate
customer data through configurable, schema-free field definitions.

**The two sides of the platform:**

- **Admin Portal** — internal staff and ETL teams configure field definitions,
  manage clients, import customer data via CSV/Excel, and monitor validation progress
- **Customer Validation Portal** — customers review their pre-populated data,
  confirm correct fields, and flag anything that needs correction

**Key design principle:** Field definitions are stored as rows in a database
table, not as database columns. Adding a new data field for a client requires
zero schema migrations — just a new row in `FieldDefinitions`.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   React Admin SPA                    │
│         (Vite + Bootstrap 5 + React Query)          │
│              http://localhost:5173                   │
└────────────────────┬────────────────────────────────┘
                     │ fetch /api/*
┌────────────────────▼────────────────────────────────┐
│              .NET 10 Web API                         │
│         (Dapper + SQL Server + Serilog)             │
│              https://localhost:7017                  │
│              Scalar UI: /scalar/v1                  │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                 SQL Server                           │
│         (Temporal tables + Triggers)                │
└─────────────────────────────────────────────────────┘
```

**Customer Portal** (not yet built — same API, separate React app on port 5174)

---

## Technology Stack

### Backend
| Concern | Technology |
|---|---|
| Framework | .NET 10, ASP.NET Core (controller-based) |
| Data access | Dapper (SQL-first, no EF Core) |
| Database | SQL Server |
| Logging | Serilog → SQL Server (split by level) |
| API docs | Scalar UI (`Microsoft.AspNetCore.OpenApi`) |
| Validation | FluentValidation |
| Authentication | Azure AD (stubbed — not active in POC) |
| CSV parsing | CsvHelper (planned) |
| Excel parsing | ClosedXML (planned) |

### Frontend — Admin SPA
| Concern | Technology |
|---|---|
| Framework | React 18, Vite |
| Language | Plain JavaScript (NO TypeScript) |
| Styling | Bootstrap 5 + SASS (DM Sans font) |
| HTTP | Native `fetch` (no axios) |
| Server state | React Query (`@tanstack/react-query`) |
| Forms | `react-hook-form` |
| Drag/drop | `@dnd-kit/core` + `@dnd-kit/sortable` |
| Routing | React Router v6 |
| Authentication | None (MSAL removed for POC phase) |

---

## Solution Structure

```
EchoCustomerValidation/
├── CLAUDE.md                                   ← Claude Code context file
├── POC.CustomerValidation/
│   └── POC.CustomerValidation.API/             ← .NET 10 Web API
│       ├── Controllers/
│       ├── Interfaces/
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Models/
│       │   ├── DTOs/
│       │   └── Entites/
│       ├── Persistence/
│       │   └── Repositories/
│       │       └── OrganizationRepository.cs
│       ├── Services/
│       ├── Startup/
│       │   └── SerilogSetup.cs
│       ├── Program.cs
│       └── appsettings.json
├── ClientAdmin/
│   └── datavalidation-portal/                  ← Admin React SPA
│       ├── src/
│       │   ├── api/
│       │   │   ├── client.js                   ← fetch wrapper
│       │   │   └── services.js                 ← API call functions
│       │   ├── components/
│       │   │   ├── common/index.jsx
│       │   │   ├── fields/FieldOptionsModal.jsx
│       │   │   └── layout/AppLayout.jsx
│       │   ├── hooks/useApi.js                 ← React Query hooks
│       │   ├── pages/
│       │   │   ├── ClientsPage.jsx
│       │   │   └── FieldDefinitionsPage.jsx
│       │   └── styles/main.scss
│       ├── package.json
│       └── vite.config.js
└── docs/                                       ← All .md documentation
    ├── README.md                               ← This file
    ├── DATABASE.md                             ← Full schema reference
    ├── API.md                                  ← All endpoints
    ├── FRONTEND.md                             ← React app conventions
    ├── IMPORT.md                               ← CSV/Excel import design
    ├── CODING_CONVENTIONS.md                   ← Rules for Claude Code
    └── WHATS_NEXT.md                           ← Remaining work
```

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- SQL Server (local or Azure SQL)

### Run both together
```bash
cd ClientAdmin/datavalidation-portal
npm install
npm run dev:all
```

### Run API only
```bash
dotnet run --project POC.CustomerValidation/POC.CustomerValidation.API \
  --launch-profile https
```
API: `https://localhost:7017`
Scalar UI: `https://localhost:7017/scalar/v1`

### Run admin React only
```bash
cd ClientAdmin/datavalidation-portal
npm run dev
```
App: `http://localhost:5173`

### Database setup
```bash
# Run scripts in this order:
1. (existing Organizations/Customers/FieldDefinitions schema)
2. SeedData.sql
3. SeedDataFieldOptions_States.sql
4. Migration_ImportTables.sql
5. Migration_AbbreviationAndImportUpdates.sql
```

---

## Documents Index

| File | Description |
|---|---|
| `DATABASE.md` | Full schema, all tables, relationships, FK diagram |
| `API.md` | All existing endpoints with request/response shapes |
| `FRONTEND.md` | React app structure, conventions, component guide |
| `IMPORT.md` | CSV/Excel import design, field mapping, CustomerCode generation |
| `CODING_CONVENTIONS.md` | Rules and patterns — critical for Claude Code sessions |
| `WHATS_NEXT.md` | Everything not yet built, prioritised |
