using POC.CustomerValidation.API.Persistence;

namespace POC.CustomerValidation.API.Middleware;

// Runs after UseRouting() so route values are populated.
// Reads the organizationId from the route and resolves the correct
// connection string (shared or isolated) for this request.
public class TenantResolutionMiddleware(
    RequestDelegate next,
    ITenantConnectionCache cache)
{
    private readonly RequestDelegate _next = next;
    private readonly ITenantConnectionCache _cache = cache;

    public async Task InvokeAsync(HttpContext context)
    {
        var orgId = ExtractOrgId(context);

        if (orgId.HasValue)
        {
            var isolatedCs = await _cache.GetIsolatedConnectionStringAsync(orgId.Value);

            if (isolatedCs is not null)
            {
                var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
                tenantContext.Resolve(isolatedCs);
            }
        }

        await _next(context);
    }

    private static Guid? ExtractOrgId(HttpContext context)
    {
        var routeValues = context.GetRouteData()?.Values;
        if (routeValues is null) return null;

        foreach (var key in new[] { "organisationId", "organizationId" })
        {
            if (routeValues.TryGetValue(key, out var val) &&
                Guid.TryParse(val?.ToString(), out var id))
                return id;
        }

        return null;
    }
}
