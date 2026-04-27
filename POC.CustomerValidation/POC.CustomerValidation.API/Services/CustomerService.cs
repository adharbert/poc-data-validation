using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class CustomerService(ICustomerRepository repo, IOrganizationRepository orgRepo) : ICustomerService
{
    private readonly ICustomerRepository     _repo    = repo;
    private readonly IOrganizationRepository _orgRepo = orgRepo;

    public async Task<PagedResult<CustomerDto>> GetByOrganisationIdAsync(
        Guid organisationId, bool includeInactive = false, int page = 1, int pageSize = 50)
    {
        var (items, total) = await _repo.GetByOrganisationIdAsync(organisationId, includeInactive, page, pageSize);
        return new PagedResult<CustomerDto>(items.Select(Map), total, page, pageSize);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid customerId)
    {
        var c = await _repo.GetByIdAsync(customerId);
        return c is null ? null : Map(c);
    }

    public async Task<CustomerDto> CreateAsync(Guid organisationId, CreateCustomerRequest request)
    {
        var org = await _orgRepo.GetByIdAsync(organisationId)
            ?? throw new KeyNotFoundException($"Organisation {organisationId} not found.");

        var abbreviation = (org.Abbreviation ?? org.OrganizationCode[..Math.Min(4, org.OrganizationCode.Length)]).ToUpperInvariant().Trim();
        var customerCode = GenerateCustomerCode(abbreviation);

        var customer = new Customer
        {
            OrganizationId  = organisationId,
            FirstName       = request.FirstName.Trim(),
            LastName        = request.LastName.Trim(),
            MiddleName      = request.MiddleName?.Trim(),
            OriginalId      = request.OriginalId?.Trim(),
            Email           = request.Email?.Trim().ToLowerInvariant(),
            CustomerCode    = customerCode,
            IsActive        = true,
        };

        var created = await _repo.CreateAsync(customer);
        return Map(created);
    }

    public async Task<CustomerDto> UpdateAsync(Guid customerId, UpdateCustomerRequest request)
    {
        var customer = await _repo.GetByIdAsync(customerId)
            ?? throw new KeyNotFoundException($"Customer {customerId} not found.");

        customer.FirstName  = request.FirstName.Trim();
        customer.LastName   = request.LastName.Trim();
        customer.MiddleName = request.MiddleName?.Trim();
        customer.OriginalId = request.OriginalId?.Trim();
        customer.Email      = request.Email?.Trim().ToLowerInvariant();
        customer.IsActive   = request.IsActive;

        await _repo.UpdateAsync(customer);
        return Map(customer);
    }

    public async Task ChangeStatusAsync(Guid customerId, bool isActive)
    {
        var ok = await _repo.ChangeStatusAsync(customerId, isActive);
        if (!ok) throw new KeyNotFoundException($"Customer {customerId} not found.");
    }

    private static string GenerateCustomerCode(string abbreviation)
    {
        var suffix = Ulid.NewUlid().ToString()[..10];
        return $"{abbreviation}-{suffix}";
    }

    private static CustomerDto Map(Customer c) => new()
    {
        CustomerId      = c.CustomerId,
        OrganizationId  = c.OrganizationId,
        FirstName       = c.FirstName,
        LastName        = c.LastName,
        MiddleName      = c.MiddleName,
        CustomerCode    = c.CustomerCode,
        OriginalId      = c.OriginalId,
        Email           = c.Email,
        IsActive        = c.IsActive ?? false,
        CreatedDate     = c.CreatedDate,
        ModifiedDate    = c.ModifiedDate,
    };
}
