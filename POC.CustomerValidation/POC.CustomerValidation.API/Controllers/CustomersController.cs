using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

/// <summary>
/// Manages customers within an organisation. CustomerCode is always system-generated.
/// OriginalId stores the client's own identifier (member number, account ID, etc.).
/// </summary>
[Route("api/organisations/{organisationId:guid}/customers")]
[ApiController]
public class CustomersController(ICustomerService service, ILogger<CustomersController> log) : ControllerBase
{
    private readonly ICustomerService _service = service;
    private readonly ILogger<CustomersController> _log = log;

    /// <summary>List customers for an organisation, paginated.</summary>
    [HttpGet]
    [EndpointSummary("Customers — list by organisation")]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        Guid organisationId,
        [FromQuery] bool includeInactive  = false,
        [FromQuery] int  page             = 1,
        [FromQuery] int  pageSize         = 50)
    {
        _log.LogInformation("GetAll customers for organisation {OrganisationId}", organisationId);
        var result = await _service.GetByOrganisationIdAsync(organisationId, includeInactive, page, pageSize);
        return Ok(result);
    }

    /// <summary>Get a single customer by Id.</summary>
    [HttpGet("{customerId:guid}")]
    [EndpointSummary("Customers — get by Id")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid organisationId, Guid customerId)
    {
        _log.LogInformation("GetById customer {CustomerId}", customerId);
        var result = await _service.GetByIdAsync(customerId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a single customer manually. CustomerCode is auto-generated.</summary>
    [HttpPost]
    [EndpointSummary("Customers — create")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid organisationId, [FromBody] CreateCustomerRequest request)
    {
        _log.LogInformation("Create customer '{FirstName} {LastName}' for organisation {OrganisationId}", request.FirstName, request.LastName, organisationId);
        var result = await _service.CreateAsync(organisationId, request);
        return CreatedAtAction(nameof(GetById), new { organisationId, customerId = result.CustomerId }, result);
    }

    /// <summary>Update a customer's details.</summary>
    [HttpPut("{customerId:guid}")]
    [EndpointSummary("Customers — update")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid organisationId, Guid customerId, [FromBody] UpdateCustomerRequest request)
    {
        _log.LogInformation("Update customer {CustomerId}", customerId);
        var result = await _service.UpdateAsync(customerId, request);
        return Ok(result);
    }

    /// <summary>Activate or deactivate a customer.</summary>
    [HttpPatch("{customerId:guid}/status")]
    [EndpointSummary("Customers — set status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid organisationId, Guid customerId, [FromBody] SetStatusRequest request)
    {
        _log.LogInformation("SetStatus customer {CustomerId} → {IsActive}", customerId, request.IsActive);
        await _service.ChangeStatusAsync(customerId, request.IsActive);
        return NoContent();
    }
}
