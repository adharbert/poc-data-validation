# Coding Conventions

Critical rules for this codebase. Claude Code must follow these
before writing any code. Deviations cause runtime errors.

---

## C# / .NET Conventions

### Dependency Injection

**Use primary constructors:**
```csharp
// CORRECT
public class OrganizationService(
    IOrganizationRepository repo,
    ILogger<OrganizationService> log)
{
    private readonly IOrganizationRepository _repo = repo;
    private readonly ILogger<OrganizationService> _log = log;
}
```

**ILogger must be generic — never plain ILogger:**
```csharp
// CORRECT
ILogger<OrganizationService> log

// WRONG — causes DI startup error
ILogger log
```

**All registrations before builder.Build():**
```csharp
// CORRECT
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
var app = builder.Build();

// WRONG — causes "service collection is read-only" error
var app = builder.Build();
app.Services.AddScoped<...>();   // ← TOO LATE
```

---

### Middleware

**Use convention-based pattern. Never IMiddleware.**

```csharp
// CORRECT — convention-based
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log  = log;
    }

    public async Task InvokeAsync(HttpContext context)  // must be InvokeAsync
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleAsync(context, ex); }
    }
}

// WRONG — IMiddleware causes "Unable to resolve RequestDelegate" error
public class ExceptionHandlingMiddleware : IMiddleware { ... }
```

**Do NOT register middleware in DI:**
```csharp
// CORRECT — just UseMiddleware, no AddTransient
app.UseMiddleware<ExceptionHandlingMiddleware>();

// WRONG — causes DI error
builder.Services.AddTransient<ExceptionHandlingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Method must be named exactly `InvokeAsync` (not `InvoiceAsync` or anything else).**

---

### Repositories (Dapper)

```csharp
public class OrganizationRepository(IDbConnectionFactory db)
    : IOrganizationRepository
{
    private readonly IDbConnectionFactory _db = db;

    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT  Id              as OrganizationId
                ,   Name            as OrganizationName
                ,   Abbreviation
                ,   IsActive
                ,   CreateUtcDt     as CreatedDate
                ,   ModifiedUtcDt   as ModifiedDate
            FROM    Organizations
            WHERE   Id = @Id
            """;

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Organization>(sql, new { Id = id });
    }
}
```

**Rules:**
- Raw SQL in verbatim string literals (`""" ... """`)
- Column aliases must match entity property names exactly
- Always `using var conn = _db.CreateConnection()` — do not keep connections open
- `IDbConnectionFactory` registered as Singleton; repositories as Scoped

---

### Services — DTO Mapping

**DTOs use non-positional records with `init` properties:**
```csharp
// CORRECT — allows object initializer syntax in Map()
public record OrganizationDto
{
    public Guid     OrganizationId   { get; init; }
    public string   OrganizationName { get; init; } = string.Empty;
    public string?  Abbreviation     { get; init; }
    public bool     IsActive         { get; init; }
}

// WRONG — positional records require argument-order constructor syntax
// which breaks easily with many fields
public record OrganizationDto(Guid OrganizationId, string OrganizationName, ...);
```

**Map via private static method:**
```csharp
private static OrganizationDto Map(Organization org) => new()
{
    OrganizationId   = org.OrganizationId,
    OrganizationName = org.OrganizationName,
    Abbreviation     = org.Abbreviation,
    IsActive         = org.IsActive ?? false,
};

// Use method group in LINQ — not a lambda
return entities.Select(Map);
```

---

### Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrganisationsController(IOrganisationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        => Ok(await service.GetAllAsync(includeInactive));
}
```

**Route scoping for field endpoints:**
```csharp
// Field definitions are scoped to a client via class-level route
[Route("api/clients/{clientId:guid}/fields")]
public class FieldDefinitionsController : ControllerBase
{
    // clientId comes from the class route
    // fieldId comes from the method route
    [HttpGet("{fieldId:guid}")]
    public async Task<IActionResult> GetById(Guid clientId, Guid fieldId)
    {
        // Always pass BOTH IDs — validate field belongs to client
        var field = await _service.GetByIdAsync(clientId, fieldId);
        return field is null ? NotFound() : Ok(field);
    }
}
```

---

### Serilog

```csharp
// Application name from config — never hardcode
var appName = context.Configuration["ApplicationName"] ?? "Unknown";

config.Enrich.WithProperty("Application", appName)
```

```json
// appsettings.json — NOT inside Serilog section
{
  "ApplicationName": "POC.CustomerValidation.API"
}
```

Two SQL tables split by log level:
- `InformationLogs` — Information only
- `ErrorLogs` — Error and Fatal only

---

### CustomerCode Generation

```csharp
public static class CustomerCodeGenerator
{
    public static string Generate(string abbreviation)
    {
        var prefix = abbreviation.ToUpperInvariant().Trim();
        var suffix = Ulid.NewUlid().ToString()[..10];
        return $"{prefix}-{suffix}";
    }
}
```

- Always generated from `Organisation.Abbreviation`
- Abbreviation max 4 chars, unique per org
- Never accept CustomerCode from CSV/Excel mapping
- Retry up to 5 times on collision

---

## JavaScript / React Conventions

### Absolutely no TypeScript
- All files use `.js` or `.jsx` — never `.ts` or `.tsx`
- No type annotations, no interfaces, no generics
- No `tsconfig.json`

### No axios
- Use the native `fetch` wrapper in `src/api/client.js`
- The wrapper returns `{ data }` — callers use `.then(r => r.data)`
- Query params via `{ params: { key: value } }` option

### File naming
- Pages: `PascalCase.jsx` — default export
- Components: `PascalCase.jsx` — named exports
- Hooks: `camelCase.js` — named exports
- API services: `camelCase.js` — named exports

### Forms
```jsx
// Numeric fields — use empty string default, convert in onSubmit
const { register } = useForm({
  defaultValues: {
    minValue: field?.minValue ?? '',   // NOT null or undefined
  }
})

// Convert in onSubmit
const payload = {
  minValue: data.minValue !== '' ? Number(data.minValue) : null,
}
```

### React Query — always use QK keys
```js
// CORRECT
qc.invalidateQueries({ queryKey: QK.fields(clientId) })

// WRONG — string keys break cache invalidation
qc.invalidateQueries({ queryKey: ['fields'] })
```

### No authentication code
MSAL has been removed. Do not add it back. No `AuthenticatedTemplate`,
no `MsalProvider`, no `loginRequest`, no Bearer token headers.
Auth will be added in a future phase.

---

## SQL Conventions

### Scripts
```sql
-- Always at top
SET NOCOUNT ON

-- Always wrap inserts/updates in transaction
BEGIN TRANSACTION
BEGIN TRY
    -- ... work ...
    COMMIT TRANSACTION
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
    -- print error details
    RAISERROR(@ErrMessage, @ErrSeverity, @ErrState)
END CATCH
```

### Guard against duplicate runs
```sql
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MyTable')
BEGIN
    RAISERROR('Already exists. Aborted.', 16, 1)
    RETURN
END
```

### Validate data before adding constraints
```sql
-- Check for violations first, show the offending rows, then abort
IF EXISTS (SELECT 1 FROM dbo.MyTable WHERE LEN(MyCol) > 4)
BEGIN
    SELECT * FROM dbo.MyTable WHERE LEN(MyCol) > 4
    RAISERROR('Fix data above before adding constraint.', 16, 1)
    RETURN
END
```

---

## Column Name Typo (do not fix)

`FieldDefinitions.DsiplayOrder` — this column has a typo in the
database (`DsiplayOrder` not `DisplayOrder`). **Do not rename it.**
The column name is established in production data. Always reference
it as `DsiplayOrder` in SQL and `DsiplayOrder` in entity/DTO mapping.
