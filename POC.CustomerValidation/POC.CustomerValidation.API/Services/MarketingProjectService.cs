using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class MarketingProjectService(
    IMarketingProjectRepository repo,
    IOrganizationRepository orgRepo) : IMarketingProjectService
{
    private readonly IMarketingProjectRepository _repo    = repo;
    private readonly IOrganizationRepository     _orgRepo = orgRepo;

    public async Task<IEnumerable<MarketingProjectDto>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false)
    {
        var org      = await _orgRepo.GetByIdAsync(organisationId);
        var projects = await _repo.GetByOrganisationIdAsync(organisationId, includeInactive);
        return projects.Select(p => Map(p, org?.OrganizationName ?? string.Empty));
    }

    public async Task<MarketingProjectDto?> GetByIdAsync(int projectId)
    {
        var project = await _repo.GetByIdAsync(projectId);
        if (project is null) return null;
        var org = await _orgRepo.GetByIdAsync(project.OrganizationId);
        return Map(project, org?.OrganizationName ?? string.Empty);
    }

    public async Task<MarketingProjectDto> CreateAsync(Guid organisationId, CreateMarketingProjectRequest request)
    {
        var org = await _orgRepo.GetByIdAsync(organisationId)
            ?? throw new KeyNotFoundException($"Organisation {organisationId} not found.");

        var project = new MarketingProject
        {
            OrganizationId      = organisationId,
            ContractId          = request.ContractId,
            ProjectName         = request.ProjectName.Trim(),
            MarketingStartDate  = request.MarketingStartDate,
            MarketingEndDate    = request.MarketingEndDate,
            IsActive            = true,
            Notes               = request.Notes?.Trim(),
            CreatedBy           = request.CreatedBy,
        };

        var created = await _repo.CreateAsync(project);
        return Map(created, org.OrganizationName);
    }

    public async Task<MarketingProjectDto> UpdateAsync(int projectId, UpdateMarketingProjectRequest request)
    {
        var project = await _repo.GetByIdAsync(projectId)
            ?? throw new KeyNotFoundException($"Project {projectId} not found.");

        project.ContractId          = request.ContractId;
        project.ProjectName         = request.ProjectName.Trim();
        project.MarketingStartDate  = request.MarketingStartDate;
        project.MarketingEndDate    = request.MarketingEndDate;
        project.IsActive            = request.IsActive;
        project.Notes               = request.Notes?.Trim();
        project.ModifiedBy          = request.ModifiedBy;

        await _repo.UpdateAsync(project);

        var org = await _orgRepo.GetByIdAsync(project.OrganizationId);
        return Map(project, org?.OrganizationName ?? string.Empty);
    }

    public async Task ChangeStatusAsync(int projectId, bool isActive, string modifiedBy)
    {
        var ok = await _repo.ChangeStatusAsync(projectId, isActive, modifiedBy);
        if (!ok) throw new KeyNotFoundException($"Project {projectId} not found.");
    }

    private static MarketingProjectDto Map(MarketingProject p, string orgName) => new()
    {
        ProjectId           = p.ProjectId,
        OrganizationId      = p.OrganizationId,
        OrganizationName    = orgName,
        ContractId          = p.ContractId,
        ProjectName         = p.ProjectName,
        MarketingStartDate  = p.MarketingStartDate,
        MarketingEndDate    = p.MarketingEndDate,
        IsActive            = p.IsActive,
        Notes               = p.Notes,
        CreatedDt           = p.CreatedDt,
        CreatedBy           = p.CreatedBy,
        ModifiedDt          = p.ModifiedDt,
        ModifiedBy          = p.ModifiedBy,
    };
}
