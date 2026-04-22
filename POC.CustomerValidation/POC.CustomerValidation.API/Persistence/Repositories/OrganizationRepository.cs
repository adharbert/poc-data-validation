using Dapper;
using POC.CustomerValidation.API.Extensions;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class OrganizationRepository(IDbConnectionFactory db) : IOrganizationRepository
{

    private readonly IDbConnectionFactory _db = db;


    public async Task<IEnumerable<Organization>> GetAllAsync(bool includeInactive = false)
    {
        const string sql = """
            SELECT	Id as OrganizationId
            		,	Name as OrganizationName
            		,	FilingName
            		,	MarketingName
            		,	Abbreviation
            		,	OrganizationCode
            		,	Website
            		,	Phone
            		,	CompanyEmail
            		,	IsActive
            		,	CreateUtcDt as CreatedDate
            		,	CreatedBy
            		,	ModifiedUtcDt as ModifiedDate
            		,	ModifiedBy
            FROM	Organizations
            WHERE	(@IncludeInactive = 1 OR IsActive = 1)
            ORDER	BY Name
        """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Organization>(sql, new { IncludeInactive = includeInactive });      

    }

    public async Task<Organization?> GetByIdAsync(Guid OrganizationId)
    {
        const string sql = """
                SELECT	Id as OrganizationId
                        ,	Name as OrganizationName
                        ,	FilingName
                        ,	MarketingName
                        ,	Abbreviation
                        ,	OrganizationCode
                        ,	Website
                        ,	Phone
                        ,	CompanyEmail
                        ,	IsActive
                        ,	CreateUtcDt as CreatedDate
                        ,	CreatedBy
                        ,	ModifiedUtcDt as ModifiedDate
                        ,	ModifiedBy
                FROM	Organizations
                WHERE	Id = @OrganizationId
            """;

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Organization>(sql, new { OrganizationId });
    }

    public async Task<Organization?> GetByOrganizationCodeAsync(string organizationCode)
    {
        const string sql = """
                SELECT	Id as OrganizationId
                        ,	Name as OrganizationName
                        ,	FilingName
                        ,	MarketingName
                        ,	Abbreviation
                        ,	OrganizationCode
                        ,	Website
                        ,	Phone
                        ,	CompanyEmail
                        ,	IsActive
                        ,	CreateUtcDt as CreatedDate
                        ,	CreatedBy
                        ,	ModifiedUtcDt as ModifiedDate
                        ,	ModifiedBy
                FROM	Organizations
                WHERE	OrganizationCode = @OrganizationCode              
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
        organization.Phone?.ToDigitsOnly();

        const string sql = """
                INSERT INTO Organizations(Id, Name, FilingName, MarketingName, Abbreviation, OrganizationCode, Website, Phone, CompanyEmail, IsActive, CreateUtcDt, CreatedBy, ModifiedUtcDt, ModifiedBy)
                VALUES (@OrganizationId, @OrganizationName, @FilingName, @MarketingName, @Abbreviation, @OrganizationCode, @Website, @Phone, @CompanyEmail, @IsActive, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, organization);
        return organization;

    }

    public async Task<bool> UpdateAsync(Organization organization)
    {
        organization.ModifiedDate = DateTime.UtcNow;

        const string sql = """
                UPDATE Organizations
                SET Name            = @OrganizationName
                    , FilingName    = @FilingName
                    , MarketingName = @MarketingName
                    , Abbreviation  = @Abbreviation
                    , Website       = @Website
                    , Phone         = @Phone
                    , CompanyEmail  = @CompanyEmail
                    , IsActive      = @IsActive
                    , ModifiedUtcDt = @ModifiedDate
                    , ModifiedBy    = @ModifiedBy
                WHERE Id            = @OrganizationId
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, organization);
        return rows > 0;
    }

    public async Task<bool> ChangeStatusAsync(Guid organizationId, bool isActive = true, string modifiedBy = "System")
    {
        const string sql = """
                UPDATE Organizations
                SET IsActive        = @IsActive
                    , ModifiedUtcDt = @ModifiedDate
                    , ModifiedBy    = @ModifiedBy
                WHERE Id = @OrganizationId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { OrganizationId = organizationId, ModifiedDate = DateTime.UtcNow, ModifiedBy = modifiedBy, @IsActive = isActive });
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(Guid organizationId)
    {
        const string sql = "SELECT COUNT(1) FROM Organizations WHERE Id = @OrganizationId";
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql , new { OrganizationId = organizationId }) > 0;
    }

}

