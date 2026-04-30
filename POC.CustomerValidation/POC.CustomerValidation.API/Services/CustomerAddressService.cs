using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class CustomerAddressService(
    ICustomerAddressRepository repo,
    ICustomerRepository customerRepo,
    IMelissaService melissa) : ICustomerAddressService
{
    private readonly ICustomerAddressRepository _repo        = repo;
    private readonly ICustomerRepository        _customerRepo = customerRepo;
    private readonly IMelissaService            _melissa     = melissa;

    public async Task<IEnumerable<CustomerAddressDto>> GetAllAsync(Guid customerId)
    {
        var addresses = await _repo.GetByCustomerIdAsync(customerId);
        return addresses.Select(Map);
    }

    public async Task<CustomerAddressDto?> GetCurrentAsync(Guid customerId)
    {
        var address = await _repo.GetCurrentAsync(customerId);
        return address is null ? null : Map(address);
    }

    public async Task<CustomerAddressDto> CreateAsync(Guid customerId, CreateCustomerAddressRequest request)
    {
        _ = await _customerRepo.GetByIdAsync(customerId)
            ?? throw new KeyNotFoundException($"Customer {customerId} not found.");

        var melissa = await _melissa.ValidateAsync(
            request.AddressLine1, request.AddressLine2,
            request.City, request.State, request.PostalCode, request.Country);

        // Use Melissa-standardised values when valid; fall back to raw input otherwise.
        var address = new CustomerAddress
        {
            CustomerId        = customerId,
            AddressLine1      = melissa.IsValid ? melissa.AddressLine1  : request.AddressLine1,
            AddressLine2      = melissa.IsValid ? melissa.AddressLine2  : request.AddressLine2,
            City              = melissa.IsValid ? melissa.City          : request.City,
            State             = melissa.IsValid ? melissa.State         : request.State,
            PostalCode        = melissa.IsValid ? melissa.PostalCode    : request.PostalCode,
            Country           = request.Country,
            MelissaValidated  = melissa.IsValid,
            CustomerConfirmed = false,
        };

        var created = await _repo.CreateAsync(address);
        return Map(created);
    }

    public async Task<CustomerAddressDto> ConfirmAsync(Guid customerId, Guid addressId)
    {
        var address = await _repo.GetByIdAsync(addressId)
            ?? throw new KeyNotFoundException($"Address {addressId} not found.");

        if (address.CustomerId != customerId)
            throw new KeyNotFoundException($"Address {addressId} does not belong to customer {customerId}.");

        await _repo.ConfirmAsync(addressId);

        var updated = await _repo.GetByIdAsync(addressId);
        return Map(updated!);
    }

    private static CustomerAddressDto Map(CustomerAddress a) => new()
    {
        AddressId         = a.AddressId,
        CustomerId        = a.CustomerId,
        AddressLine1      = a.AddressLine1,
        AddressLine2      = a.AddressLine2,
        City              = a.City,
        State             = a.State,
        PostalCode        = a.PostalCode,
        Country           = a.Country,
        MelissaValidated  = a.MelissaValidated,
        CustomerConfirmed = a.CustomerConfirmed,
        IsCurrent         = a.IsCurrent,
        CreatedUtcDt      = a.CreatedUtcDt,
        ModifiedUtcDt     = a.ModifiedUtcDt,
    };
}
