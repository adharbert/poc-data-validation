using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;
using Serilog;
using static Dapper.SqlMapper;

namespace POC.CustomerValidation.API.Services;

public class OrganizationServices(IOrganizationRepository organizationRepository) : IOrganizationServices
{
    private readonly IOrganizationRepository _repo = organizationRepository;



    public async Task<IEnumerable<OrganizationDto>> GetAllAsync(bool includeInactive = false)
    {
        var orgs = await _repo.GetAllAsync(includeInactive);

        // this call below is the equivalent of "orgs.Select(org => Map(org));"
        return orgs.Select(Map);

    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid organizationId)
    {
        var org = await _repo.GetByIdAsync(organizationId);
        return org is null ? null : Map(org);
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request)
    {
        // Make sure the organization doesn't already exist.
        var existing = await _repo.GetByIdAsync(request.OrganizationId);
        if (existing is not null)
            throw new InvalidOperationException($"Organization with ID {request.OrganizationId} already exists.");
        
        var org = Map(request);
        var newOrg = await _repo.CreateAsync(org);

        return Map(newOrg);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid organizationId, UpdateOrganizationRequest request)
    {
        var org = await _repo.GetByIdAsync(organizationId)
            ?? throw new KeyNotFoundException($"Organization {organizationId} not found.");

        MapToEntity(request, org);
        await _repo.UpdateAsync(org);

        return Map(org);
    }

    public async Task ChangeStatus(Guid organizationId, bool isActive)
    {
        var ok = await _repo.ChangeStatusAsync(organizationId, isActive);
        if (!ok) 
            throw new KeyNotFoundException($"Organization {organizationId} not found.");
    }




    private static Organization Map(CreateOrganizationRequest request)
    {
        return new Organization
        {
            OrganizationId      = request.OrganizationId,
            OrganizationName    = request.OrganizationName,
            FilingName          = request.FilingName,
            MarketingName       = request.MarketingName,
            Abbreviation        = request.Abbreviation,
            Website             = request.Website,
            Phone               = request.Phone,
            CompanyEmail        = request.CompanyEmail,
            IsActive            = true, // New organizations are active by default
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
            org.CreatedDate ?? DateTime.MinValue,
            org.CreatedBy,
            org.ModifiedDate ?? DateTime.MinValue,
            org.ModifiedBy
        );
    }


    private static void MapToEntity(UpdateOrganizationRequest request, Organization org)
    {
        org.OrganizationName    = request.OrganizationName.Trim();
        org.OrganizationCode    = request.OrganizationCode.Trim();
        org.FilingName          = request.FilingName?.Trim();
        org.MarketingName       = request.MarketingName?.Trim();
        org.Abbreviation        = request.Abbreviation?.Trim();
        org.Website             = request.Website?.Trim();
        org.Phone               = request.Phone?.Trim();
        org.CompanyEmail        = request.CompanyEmail?.Trim();
        org.IsActive            = request.IsActive ?? org.IsActive;
    }

}
