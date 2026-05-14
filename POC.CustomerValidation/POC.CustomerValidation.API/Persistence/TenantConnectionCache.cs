using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;

namespace POC.CustomerValidation.API.Persistence;

// Singleton — caches org → isolated connection string lookups so we don't hit the
// shared DB on every request. Invalidated when provisioning completes.
public interface ITenantConnectionCache
{
    Task<string?> GetIsolatedConnectionStringAsync(Guid organizationId);
    void Invalidate(Guid organizationId);
}

public class TenantConnectionCache(IConfiguration configuration) : ITenantConnectionCache
{
    private readonly string _sharedConnectionString = configuration.GetConnectionString("DefaultConnection")!;
    private readonly ConcurrentDictionary<Guid, string?> _cache = new();

    public async Task<string?> GetIsolatedConnectionStringAsync(Guid organizationId)
    {
        if (_cache.TryGetValue(organizationId, out var cached))
            return cached;

        const string sql = """
            SELECT  IsolatedConnectionString
            FROM    Organizations
            WHERE   Id = @OrganizationId
              AND   DatabaseProvisioningStatus = 'ready'
        """;

        using var conn = new SqlConnection(_sharedConnectionString);
        var cs = await conn.QuerySingleOrDefaultAsync<string?>(sql, new { OrganizationId = organizationId });

        _cache[organizationId] = cs;
        return cs;
    }

    public void Invalidate(Guid organizationId) => _cache.TryRemove(organizationId, out _);
}
