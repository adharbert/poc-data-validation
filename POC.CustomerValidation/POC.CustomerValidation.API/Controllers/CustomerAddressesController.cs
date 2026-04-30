using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

/// <summary>
/// Manages the address history for a customer.
/// POST creates a new address (calls Melissa and retires the previous one).
/// PATCH /current/confirm lets the customer confirm the address is correct.
/// </summary>
[Route("api/customers/{customerId:guid}/addresses")]
[ApiController]
public class CustomerAddressesController(
    ICustomerAddressService service,
    ILogger<CustomerAddressesController> log) : ControllerBase
{
    private readonly ICustomerAddressService            _service = service;
    private readonly ILogger<CustomerAddressesController> _log   = log;

    /// <summary>List all addresses for a customer (full history, newest first).</summary>
    [HttpGet]
    [EndpointSummary("Customer Addresses — list history")]
    [ProducesResponseType(typeof(IEnumerable<CustomerAddressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid customerId)
    {
        _log.LogInformation("GetAll addresses for customer {CustomerId}", customerId);
        return Ok(await _service.GetAllAsync(customerId));
    }

    /// <summary>Get the customer's current address.</summary>
    [HttpGet("current")]
    [EndpointSummary("Customer Addresses — get current")]
    [ProducesResponseType(typeof(CustomerAddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(Guid customerId)
    {
        _log.LogInformation("GetCurrent address for customer {CustomerId}", customerId);
        var result = await _service.GetCurrentAsync(customerId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Add a new address for the customer.
    /// Submits to Melissa for validation, then sets as the current address.
    /// The previous address is retained in history.
    /// </summary>
    [HttpPost]
    [EndpointSummary("Customer Addresses — create / move")]
    [ProducesResponseType(typeof(CustomerAddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid customerId, [FromBody] CreateCustomerAddressRequest request)
    {
        _log.LogInformation("Create address for customer {CustomerId}", customerId);
        var result = await _service.CreateAsync(customerId, request);
        return CreatedAtAction(nameof(GetCurrent), new { customerId }, result);
    }

    /// <summary>Customer confirms the current address is correct.</summary>
    [HttpPatch("{addressId:guid}/confirm")]
    [EndpointSummary("Customer Addresses — confirm")]
    [ProducesResponseType(typeof(CustomerAddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(Guid customerId, Guid addressId)
    {
        _log.LogInformation("Confirm address {AddressId} for customer {CustomerId}", addressId, customerId);
        var result = await _service.ConfirmAsync(customerId, addressId);
        return Ok(result);
    }
}
