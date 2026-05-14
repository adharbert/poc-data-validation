using Microsoft.Data.SqlClient;
using System.Data;

namespace POC.CustomerValidation.API.Persistence;

public class TenantAwareConnectionFactory(ITenantContext tenantContext) : IDbConnectionFactory
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public IDbConnection CreateConnection() => new SqlConnection(_tenantContext.ConnectionString);
}
