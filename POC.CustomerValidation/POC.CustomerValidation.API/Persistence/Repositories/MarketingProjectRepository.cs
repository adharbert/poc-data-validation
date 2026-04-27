using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class MarketingProjectRepository(IDbConnectionFactory db) : IMarketingProjectRepository
{
    private readonly IDbConnectionFactory _db = db;

    public async Task<IEnumerable<MarketingProject>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false)
    {
        const string sql = """
            SELECT  Id                  AS ProjectId
                ,   OrganizationId
                ,   ContractId
                ,   ProjectName
                ,   MarketingStartDate
                ,   MarketingEndDate
                ,   IsActive
                ,   Notes
                ,   CreatedDt
                ,   CreatedBy
                ,   ModifiedDt
                ,   ModifiedBy
            FROM    MarketingProjects
            WHERE   OrganizationId      = @OrganisationId
              AND   (@IncludeInactive   = 1 OR IsActive = 1)
            ORDER BY IsActive DESC, MarketingStartDate DESC
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<MarketingProject>(sql, new { OrganisationId = organisationId, IncludeInactive = includeInactive });
    }

    public async Task<MarketingProject?> GetByIdAsync(int projectId)
    {
        const string sql = """
            SELECT  Id                  AS ProjectId
                ,   OrganizationId
                ,   ContractId
                ,   ProjectName
                ,   MarketingStartDate
                ,   MarketingEndDate
                ,   IsActive
                ,   Notes
                ,   CreatedDt
                ,   CreatedBy
                ,   ModifiedDt
                ,   ModifiedBy
            FROM    MarketingProjects
            WHERE   Id = @ProjectId
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<MarketingProject>(sql, new { ProjectId = projectId });
    }

    public async Task<MarketingProject> CreateAsync(MarketingProject project)
    {
        project.CreatedDt = DateTime.UtcNow;

        const string sql = """
            INSERT INTO MarketingProjects
                (OrganizationId, ContractId, ProjectName, MarketingStartDate, MarketingEndDate, IsActive, Notes, CreatedDt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES
                (@OrganizationId, @ContractId, @ProjectName, @MarketingStartDate, @MarketingEndDate, @IsActive, @Notes, @CreatedDt, @CreatedBy)
            """;
        using var conn = _db.CreateConnection();
        project.ProjectId = await conn.ExecuteScalarAsync<int>(sql, project);
        return project;
    }

    public async Task<bool> UpdateAsync(MarketingProject project)
    {
        project.ModifiedDt = DateTime.UtcNow;

        const string sql = """
            UPDATE  MarketingProjects
            SET     ContractId          = @ContractId
                ,   ProjectName         = @ProjectName
                ,   MarketingStartDate  = @MarketingStartDate
                ,   MarketingEndDate    = @MarketingEndDate
                ,   IsActive            = @IsActive
                ,   Notes               = @Notes
                ,   ModifiedDt          = @ModifiedDt
                ,   ModifiedBy          = @ModifiedBy
            WHERE   Id = @ProjectId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, project);
        return rows > 0;
    }

    public async Task<bool> ChangeStatusAsync(int projectId, bool isActive, string modifiedBy)
    {
        const string sql = """
            UPDATE  MarketingProjects
            SET     IsActive    = @IsActive
                ,   ModifiedDt  = @ModifiedDt
                ,   ModifiedBy  = @ModifiedBy
            WHERE   Id = @ProjectId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { ProjectId = projectId, IsActive = isActive, ModifiedDt = DateTime.UtcNow, ModifiedBy = modifiedBy });
        return rows > 0;
    }
}
