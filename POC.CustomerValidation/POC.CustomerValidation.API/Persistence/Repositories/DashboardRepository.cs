using Dapper;
using POC.CustomerValidation.API.Interfaces;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class DashboardRepository(IDbConnectionFactory db) : IDashboardRepository
{
    private readonly IDbConnectionFactory _db = db;

    public async Task<DashboardStatsRaw> GetStatsAsync()
    {
        const string sql = """
            SELECT
                (SELECT COUNT(1) FROM Organizations WHERE IsActive = 1)                                     AS TotalActiveOrganizations
            ,   (SELECT COUNT(1) FROM MarketingProjects WHERE IsActive = 1)                                 AS TotalActiveProjects
            ,   (SELECT COUNT(1) FROM Customers WHERE IsActive = 1)                                         AS TotalCustomers
            ,   (SELECT COUNT(DISTINCT CustomerId) FROM FieldValues WHERE ConfirmedAt IS NOT NULL)          AS TotalVerifiedCustomers
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<DashboardStatsRaw>(sql);
    }

    public async Task<IEnumerable<OrganisationCustomerSummary>> GetOrganisationSummariesAsync()
    {
        const string sql = """
            SELECT  o.Id                                                                    AS OrganisationId
                ,   o.Name                                                                  AS OrganisationName
                ,   COUNT(DISTINCT c.Id)                                                    AS TotalCustomers
                ,   COUNT(DISTINCT CASE WHEN fv.ConfirmedAt IS NOT NULL THEN c.Id END)      AS VerifiedCustomers
                ,   (SELECT COUNT(1) FROM MarketingProjects mp
                        WHERE mp.OrganizationId = o.Id AND mp.IsActive = 1)                 AS ActiveProjects
            FROM    Organizations o
            LEFT JOIN Customers c   ON c.OrganizationId = o.Id AND c.IsActive = 1
            LEFT JOIN FieldValues fv ON fv.CustomerId = c.Id
            WHERE   o.IsActive = 1
            GROUP BY o.Id, o.Name
            ORDER BY o.Name
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<OrganisationCustomerSummary>(sql);
    }

    public async Task<IEnumerable<ExpiringProjectRow>> GetExpiringProjectsAsync(int warningDays)
    {
        const string sql = """
            SELECT  mp.Id                                               AS ProjectId
                ,   mp.ProjectName
                ,   o.Id                                                AS OrganisationId
                ,   o.Name                                              AS OrganisationName
                ,   mp.MarketingEndDate
                ,   DATEDIFF(day, CAST(GETUTCDATE() AS date), mp.MarketingEndDate) AS DaysUntilExpiry
            FROM    MarketingProjects mp
            INNER JOIN Organizations o ON o.Id = mp.OrganizationId
            WHERE   mp.IsActive             = 1
              AND   mp.MarketingEndDate      IS NOT NULL
              AND   mp.MarketingEndDate      BETWEEN CAST(GETUTCDATE() AS date)
                                            AND     DATEADD(day, @WarningDays, CAST(GETUTCDATE() AS date))
            ORDER BY mp.MarketingEndDate ASC
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ExpiringProjectRow>(sql, new { WarningDays = warningDays });
    }
}
