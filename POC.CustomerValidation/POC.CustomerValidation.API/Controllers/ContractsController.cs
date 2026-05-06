using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

/// <summary>
/// Manages contracts for an organisation. Only one contract may be active per organisation at a time.
/// Contracts track the formal engagement lifecycle; deactivate before creating a new one.
/// </summary>
[Route("api/organisations/{organisationId:guid}/contracts")]
[ApiController]
public class ContractsController(IContractService service, ILogger<ContractsController> log) : ControllerBase
{
    private readonly IContractService _service = service;
    private readonly ILogger<ContractsController> _log = log;

    /// <summary>List all contracts for an organisation. Active contract is returned first.</summary>
    [HttpGet]
    [EndpointSummary("Contracts — list by organisation")]
    [ProducesResponseType(typeof(IEnumerable<ContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid organisationId, [FromQuery] bool includeInactive = false)
    {
        _log.LogInformation("GetAll contracts for organisation {OrganisationId}", organisationId);
        var results = await _service.GetByOrganisationIdAsync(organisationId, includeInactive);
        return Ok(results);
    }

    /// <summary>Get a single contract by Id.</summary>
    [HttpGet("{contractId:guid}")]
    [EndpointSummary("Contracts — get by Id")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid organisationId, Guid contractId)
    {
        _log.LogInformation($"GetById contract Id: {contractId} for organization Id {organisationId}");
        var result = await _service.GetByIdAsync(contractId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new contract. Fails if an active contract already exists for the organisation.</summary>
    [HttpPost]
    [EndpointSummary("Contracts — create")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid organisationId, [FromBody] CreateContractRequest request)
    {
        _log.LogInformation("Create contract '{ContractName}' for organisation {OrganisationId}", request.ContractName, organisationId);
        var result = await _service.CreateAsync(organisationId, request);
        return CreatedAtAction(nameof(GetById), new { organisationId, contractId = result.ContractId }, result);
    }

    /// <summary>Update a contract's details.</summary>
    [HttpPut("{contractId:guid}")]
    [EndpointSummary("Contracts — update")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid organisationId, Guid contractId, [FromBody] UpdateContractRequest request)
    {
        _log.LogInformation($"Update contract {contractId} for organisation {organisationId}");
        var result = await _service.UpdateAsync(contractId, request);
        return Ok(result);
    }

    /// <summary>Activate or deactivate a contract.</summary>
    [HttpPatch("{contractId:guid}/status")]
    [EndpointSummary("Contracts — set status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetStatus(Guid organisationId, Guid contractId, [FromBody] SetStatusRequest request)
    {
        _log.LogInformation($"SetStatus contract {contractId} → {request.IsActive} for organisation {organisationId}");
        await _service.ChangeStatusAsync(contractId, request.IsActive, request.ModifiedBy);
        return NoContent();
    }
}


