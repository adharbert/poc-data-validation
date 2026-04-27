using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Services;

public class DashboardService(IDashboardRepository repo) : IDashboardService
{
    private readonly IDashboardRepository _repo = repo;

    public async Task<DashboardStatsDto> GetStatsAsync(int warningDays)
    {
        var stats      = await _repo.GetStatsAsync();
        var summaries  = await _repo.GetOrganisationSummariesAsync();
        var expiring   = await _repo.GetExpiringProjectsAsync(warningDays);

        return new DashboardStatsDto
        {
            TotalActiveOrganizations    = stats.TotalActiveOrganizations,
            TotalActiveProjects         = stats.TotalActiveProjects,
            TotalCustomers              = stats.TotalCustomers,
            TotalVerifiedCustomers      = stats.TotalVerifiedCustomers,
            OrganisationSummaries       = summaries.Select(s => new OrganisationSummaryDto
            {
                OrganisationId      = s.OrganisationId,
                OrganisationName    = s.OrganisationName,
                TotalCustomers      = s.TotalCustomers,
                VerifiedCustomers   = s.VerifiedCustomers,
                ActiveProjects      = s.ActiveProjects,
            }),
            ExpiringProjects = expiring.Select(e => new ExpiringProjectDto
            {
                ProjectId           = e.ProjectId,
                ProjectName         = e.ProjectName,
                OrganisationId      = e.OrganisationId,
                OrganisationName    = e.OrganisationName,
                MarketingEndDate    = e.MarketingEndDate,
                DaysUntilExpiry     = e.DaysUntilExpiry,
            }),
        };
    }

    public async Task<IEnumerable<ExpiringProjectDto>> GetExpiringProjectsAsync(int warningDays)
    {
        var rows = await _repo.GetExpiringProjectsAsync(warningDays);
        return rows.Select(e => new ExpiringProjectDto
        {
            ProjectId           = e.ProjectId,
            ProjectName         = e.ProjectName,
            OrganisationId      = e.OrganisationId,
            OrganisationName    = e.OrganisationName,
            MarketingEndDate    = e.MarketingEndDate,
            DaysUntilExpiry     = e.DaysUntilExpiry,
        });
    }
}
