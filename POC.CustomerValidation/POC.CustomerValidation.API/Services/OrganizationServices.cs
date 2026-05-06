using POC.CustomerValidation.API.Extensions;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;
using POC.CustomerValidation.API.Services.Provisioning;

namespace POC.CustomerValidation.API.Services;

public class OrganizationServices(
    IOrganizationRepository organizationRepository,
    IProvisioningQueue provisioningQueue) : IOrganizationServices
{
    private readonly IOrganizationRepository _repo = organizationRepository;
    private readonly IProvisioningQueue _provisioningQueue = provisioningQueue;


    public async Task<IEnumerable<OrganizationDto>> GetAllAsync(bool includeInactive = false, string? search = null)
    {
        var orgs = await _repo.GetAllAsync(includeInactive, string.IsNullOrWhiteSpace(search) ? null : search.Trim());
        return orgs.Select(Map);
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid organizationId)
    {
        var org = await _repo.GetByIdAsync(organizationId);
        return org is null ? null : Map(org);
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request)
    {
        var org = Map(request);
        var newOrg = await _repo.CreateAsync(org);

        if (newOrg.RequiresIsolatedDatabase)
            EnqueueProvisioning(newOrg.OrganizationId);

        return Map(newOrg);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid organizationId, UpdateOrganizationRequest request)
    {
        var org = await _repo.GetByIdAsync(organizationId)
            ?? throw new KeyNotFoundException($"Organization {organizationId} not found.");

        // Enqueue only when transitioning to isolated for the first time OR retrying after failure.
        // Do not re-enqueue if already pending, provisioning, or ready.
        var shouldProvision = request.RequiresIsolatedDatabase &&
            org.DatabaseProvisioningStatus is null or "failed";

        MapToEntity(request, org);
        await _repo.UpdateAsync(org);

        if (shouldProvision)
            EnqueueProvisioning(org.OrganizationId);

        return Map(org);
    }

    public async Task ChangeStatus(Guid organizationId, bool isActive)
    {
        var ok = await _repo.ChangeStatusAsync(organizationId, isActive);
        if (!ok)
            throw new KeyNotFoundException($"Organization {organizationId} not found.");
    }


    public async Task ReprovisionAsync(Guid organizationId)
    {
        var org = await _repo.GetByIdAsync(organizationId)
            ?? throw new KeyNotFoundException($"Organization {organizationId} not found.");

        if (!org.RequiresIsolatedDatabase)
            throw new InvalidOperationException("Organization does not require an isolated database.");

        await _repo.UpdateProvisioningStatusAsync(organizationId, "pending");
        EnqueueProvisioning(organizationId);
    }

    private void EnqueueProvisioning(Guid organizationId)
    {
        _provisioningQueue.Enqueue(organizationId);
    }


    private static Organization Map(CreateOrganizationRequest request)
    {
        return new Organization
        {
            OrganizationName            = request.OrganizationName,
            FilingName                  = request.FilingName,
            MarketingName               = request.MarketingName,
            Abbreviation                = request.Abbreviation,
            Website                     = request.Website,
            Phone                       = request.Phone,
            CompanyEmail                = request.CompanyEmail,
            IsActive                    = true,
            RequiresIsolatedDatabase    = request.RequiresIsolatedDatabase,
            DatabaseProvisioningStatus  = request.RequiresIsolatedDatabase ? "pending" : null,
        };
    }

    private static OrganizationDto Map(Organization org)
    {
        return new OrganizationDto(
            org.OrganizationId,
            org.OrganizationName,
            org.OrganizationCode,
            org.FilingName,
            org.MarketingName,
            org.Abbreviation,
            org.Website,
            org.Phone,
            org.CompanyEmail,
            org.IsActive ?? false,
            org.RequiresIsolatedDatabase,
            org.DatabaseProvisioningStatus,
            org.CreatedDate ?? DateTime.MinValue,
            org.CreatedBy,
            org.ModifiedDate ?? DateTime.MinValue,
            org.ModifiedBy
        );
    }

    private static void MapToEntity(UpdateOrganizationRequest request, Organization org)
    {
        org.OrganizationName            = request.OrganizationName.Trim();
        org.FilingName                  = request.FilingName?.Trim();
        org.MarketingName               = request.MarketingName?.Trim();
        org.Abbreviation                = request.Abbreviation?.Trim();
        org.Website                     = request.Website?.Trim();
        org.Phone                       = request.Phone?.ToDigitsOnly();
        org.CompanyEmail                = request.CompanyEmail?.Trim();
        org.IsActive                    = request.IsActive ?? org.IsActive;
        org.RequiresIsolatedDatabase    = request.RequiresIsolatedDatabase;

        if (request.RequiresIsolatedDatabase && org.IsolatedConnectionString is null)
            org.DatabaseProvisioningStatus = "pending";
    }
}
