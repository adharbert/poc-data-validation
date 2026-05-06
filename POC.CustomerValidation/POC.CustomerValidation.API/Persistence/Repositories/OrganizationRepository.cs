using Dapper;
using POC.CustomerValidation.API.Extensions;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class OrganizationRepository(IDbConnectionFactory db) : IOrganizationRepository
{

    private readonly IDbConnectionFactory _db = db;

    private const string SelectColumns = """
            Id as OrganizationId
            ,   Name as OrganizationName
            ,   FilingName
            ,   MarketingName
            ,   Abbreviation
            ,   OrganizationCode
            ,   Website
            ,   Phone
            ,   CompanyEmail
            ,   IsActive
            ,   RequiresIsolatedDatabase
            ,   IsolatedConnectionString
            ,   DatabaseProvisioningStatus
            ,   CreateUtcDt as CreatedDate
            ,   CreatedBy
            ,   ModifiedUtcDt as ModifiedDate
            ,   ModifiedBy
        """;

    public async Task<IEnumerable<Organization>> GetAllAsync(bool includeInactive = false, string? search = null)
    {
        var sql = $"""
            SELECT  {SelectColumns}
            FROM    Organizations
            WHERE   (@IncludeInactive = 1 OR IsActive = 1)
              AND   (@Search IS NULL OR Name LIKE '%' + @Search + '%'
                                    OR Abbreviation LIKE '%' + @Search + '%'
                                    OR OrganizationCode LIKE '%' + @Search + '%')
            ORDER   BY Name
        """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Organization>(sql, new { IncludeInactive = includeInactive, Search = search });
    }

    public async Task<Organization?> GetByIdAsync(Guid OrganizationId)
    {
        var sql = $"""
            SELECT  {SelectColumns}
            FROM    Organizations
            WHERE   Id = @OrganizationId
        """;

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Organization>(sql, new { OrganizationId });
    }

    public async Task<Organization?> GetByOrganizationCodeAsync(string organizationCode)
    {
        var sql = $"""
            SELECT  {SelectColumns}
            FROM    Organizations
            WHERE   OrganizationCode = @OrganizationCode
        """;

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Organization>(sql, new { OrganizationCode = organizationCode });
    }

    public async Task<Organization> CreateAsync(Organization organization)
    {
        organization.OrganizationId     = Guid.NewGuid();
        organization.OrganizationCode   = Ulid.NewUlid().ToString();
        organization.CreatedDate        = DateTime.UtcNow;
        organization.ModifiedDate       = DateTime.UtcNow;
        organization.CreatedBy          = "System";
        organization.ModifiedBy         = "System";
        organization.Phone              = organization.Phone?.ToDigitsOnly();

        const string sql = """
            INSERT INTO Organizations(
                Id, Name, FilingName, MarketingName, Abbreviation, OrganizationCode,
                Website, Phone, CompanyEmail, IsActive, RequiresIsolatedDatabase,
                CreateUtcDt, CreatedBy, ModifiedUtcDt, ModifiedBy)
            VALUES (
                @OrganizationId, @OrganizationName, @FilingName, @MarketingName, @Abbreviation, @OrganizationCode,
                @Website, @Phone, @CompanyEmail, @IsActive, @RequiresIsolatedDatabase,
                @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)
        """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, organization);
        return organization;
    }

    public async Task<bool> UpdateAsync(Organization organization)
    {
        organization.ModifiedDate = DateTime.UtcNow;

        const string sql = """
            UPDATE  Organizations
            SET     Name                        = @OrganizationName
                ,   FilingName                  = @FilingName
                ,   MarketingName               = @MarketingName
                ,   Abbreviation                = @Abbreviation
                ,   Website                     = @Website
                ,   Phone                       = @Phone
                ,   CompanyEmail                = @CompanyEmail
                ,   IsActive                    = @IsActive
                ,   RequiresIsolatedDatabase    = @RequiresIsolatedDatabase
                ,   ModifiedUtcDt               = @ModifiedDate
                ,   ModifiedBy                  = @ModifiedBy
            WHERE   Id                          = @OrganizationId
        """;

        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, organization);
        return rows > 0;
    }

    public async Task<bool> ChangeStatusAsync(Guid organizationId, bool isActive = true, string modifiedBy = "System")
    {
        const string sql = """
            UPDATE  Organizations
            SET     IsActive        = @IsActive
                ,   ModifiedUtcDt   = @ModifiedDate
                ,   ModifiedBy      = @ModifiedBy
            WHERE   Id              = @OrganizationId
        """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { OrganizationId = organizationId, ModifiedDate = DateTime.UtcNow, ModifiedBy = modifiedBy, @IsActive = isActive });
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(Guid organizationId)
    {
        const string sql = "SELECT COUNT(1) FROM Organizations WHERE Id = @OrganizationId";
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId }) > 0;
    }

    public async Task<IEnumerable<Organization>> GetAllIsolatedAsync()
    {
        var sql = $"""
            SELECT  {SelectColumns}
            FROM    Organizations
            WHERE   RequiresIsolatedDatabase    = 1
              AND   DatabaseProvisioningStatus  = 'ready'
              AND   IsolatedConnectionString    IS NOT NULL
            ORDER   BY Name
        """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Organization>(sql);
    }

    public async Task UpdateProvisioningStatusAsync(Guid organizationId, string status, string? connectionString = null)
    {
        const string sql = """
            UPDATE  Organizations
            SET     DatabaseProvisioningStatus  = @Status
                ,   IsolatedConnectionString    = CASE WHEN @ConnectionString IS NOT NULL THEN @ConnectionString ELSE IsolatedConnectionString END
                ,   ModifiedUtcDt               = @ModifiedDate
                ,   ModifiedBy                  = 'Provisioning'
            WHERE   Id                          = @OrganizationId
        """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            OrganizationId  = organizationId,
            Status          = status,
            ConnectionString = connectionString,
            ModifiedDate    = DateTime.UtcNow
        });
    }
}
