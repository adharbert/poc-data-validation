# CLAUDE.md — Customer Validation Portal

This file provides context for Claude Code sessions working on the
POC Customer Validation project. Read this before making any changes.

---

## Project overview

A multi-tenant platform for collecting and validating customer data through
configurable, schema-free field definitions. Admins configure fields per org;
customers review and confirm their pre-populated data through a validation portal.

---

## Detailed documentation

All detailed reference lives in `TASK-FILES/`. Start there for anything not
covered by the quick-reference rules below.

| File | What's in it |
|---|---|
| [TASK-FILES/README.md](TASK-FILES/README.md) | Architecture, tech stack, solution structure, getting started |
| [TASK-FILES/TASK-01-project-structure.md](TASK-FILES/TASK-01-project-structure.md) | Project structure overview |
| [TASK-FILES/TASK-02-PROJECT_PLAN.md](TASK-FILES/TASK-02-PROJECT_PLAN.md) | Phased build plan with tasks and dependencies |
| [TASK-FILES/TASK-03-CODING_CONVENTIONS.md](TASK-FILES/TASK-03-CODING_CONVENTIONS.md) | All rules — must read before writing any code |
| [TASK-FILES/TASK-04-DATABASE.md](TASK-FILES/TASK-04-DATABASE.md) | Full schema — all tables, columns, constraints, import tables |
| [TASK-FILES/TASK-05-API.md](TASK-FILES/TASK-05-API.md) | All endpoints (existing + planned) with request/response shapes |
| [TASK-FILES/TASK-06-FRONTEND.md](TASK-FILES/TASK-06-FRONTEND.md) | React app structure, component guide, API client pattern |
| [TASK-FILES/TASK-07-IMPORT.md](TASK-FILES/TASK-07-IMPORT.md) | CSV/Excel import design, 5-step flow, value mapping |
| [TASK-FILES/TASK-08-WHATS_NEXT.md](TASK-FILES/TASK-08-WHATS_NEXT.md) | Remaining work, prioritised |
| [TASK-FILES/TASK-09-CONTRACTS-PROJECTS.md](TASK-FILES/TASK-09-CONTRACTS-PROJECTS.md) | Contracts, Marketing Projects, Import Staging — business rules and schema |

---

## Quick-start

```bash
# Both API + React
cd ClientAdmin/datavalidation-portal && npm run dev:all

# API only  — https://localhost:7124  (Scalar: /scalar/v1)
dotnet run --project POC.CustomerValidation/POC.CustomerValidation.API --launch-profile https

# React admin only — http://localhost:5173
cd ClientAdmin/datavalidation-portal && npm run dev
```

---

## Critical rules (must follow — violations cause runtime errors)

### C# / .NET

- **Primary constructors** for DI — `public class Foo(IBar bar)` not constructor injection
- **`ILogger<T>` only** — never plain `ILogger` (causes DI startup failure)
- **All DI registrations before `builder.Build()`** — never against `app.Services`
- **Middleware: convention-based only** — implement `InvokeAsync(HttpContext)`, do NOT use `IMiddleware`, do NOT register with `AddTransient`
- **DTOs: non-positional records with `init` properties** — not positional constructors
- **Repositories return entities; services return DTOs** — map via private static `Map()` methods
- **`IDbConnectionFactory` → Singleton; repositories → Scoped**
- **Dapper + raw SQL in verbatim string literals** (`""" ... """`); column aliases must match entity property names

### React / JavaScript

- **Plain JavaScript only** — no TypeScript, no `.ts`/`.tsx` files
- **No axios** — use the `fetch` wrapper at `src/api/client.js`
- **Always use `QK` object** for React Query cache keys — never raw string arrays
- **No MSAL / auth code** — removed for POC, do not add back
- **Numeric form fields** use empty string defaults, converted to `null` in `onSubmit`

### SQL scripts

- `SET NOCOUNT ON` at top
- Wrap all inserts/updates in `BEGIN TRANSACTION` / `BEGIN TRY` / `BEGIN CATCH`
- Guard against duplicate runs before inserting

