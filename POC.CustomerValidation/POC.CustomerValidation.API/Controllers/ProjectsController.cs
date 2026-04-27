using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

/// <summary>
/// Manages marketing projects for an organisation. Project IDs are integers starting at 8000.
/// Multiple projects can be active simultaneously under the same organisation.
/// </summary>
[Route("api/organisations/{organisationId:guid}/projects")]
[ApiController]
public class ProjectsController(IMarketingProjectService service, ILogger<ProjectsController> log) : ControllerBase
{
    private readonly IMarketingProjectService _service = service;
    private readonly ILogger<ProjectsController> _log  = log;

    /// <summary>List all marketing projects for an organisation.</summary>
    [HttpGet]
    [EndpointSummary("Projects — list by organisation")]
    [ProducesResponseType(typeof(IEnumerable<MarketingProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid organisationId, [FromQuery] bool includeInactive = false)
    {
        _log.LogInformation("GetAll projects for organisation {OrganisationId}", organisationId);
        var results = await _service.GetByOrganisationIdAsync(organisationId, includeInactive);
        return Ok(results);
    }

    /// <summary>Get a single marketing project by its integer project Id.</summary>
    [HttpGet("{projectId:int}")]
    [EndpointSummary("Projects — get by Id")]
    [ProducesResponseType(typeof(MarketingProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid organisationId, int projectId)
    {
        _log.LogInformation("GetById project {ProjectId}", projectId);
        var result = await _service.GetByIdAsync(projectId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new marketing project. Project ID is auto-assigned starting at 8000.</summary>
    [HttpPost]
    [EndpointSummary("Projects — create")]
    [ProducesResponseType(typeof(MarketingProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid organisationId, [FromBody] CreateMarketingProjectRequest request)
    {
        _log.LogInformation("Create project '{ProjectName}' for organisation {OrganisationId}", request.ProjectName, organisationId);
        var result = await _service.CreateAsync(organisationId, request);
        return CreatedAtAction(nameof(GetById), new { organisationId, projectId = result.ProjectId }, result);
    }

    /// <summary>Update a marketing project's details.</summary>
    [HttpPut("{projectId:int}")]
    [EndpointSummary("Projects — update")]
    [ProducesResponseType(typeof(MarketingProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid organisationId, int projectId, [FromBody] UpdateMarketingProjectRequest request)
    {
        _log.LogInformation("Update project {ProjectId}", projectId);
        var result = await _service.UpdateAsync(projectId, request);
        return Ok(result);
    }

    /// <summary>Activate or deactivate a marketing project.</summary>
    [HttpPatch("{projectId:int}/status")]
    [EndpointSummary("Projects — set status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid organisationId, int projectId, [FromBody] SetStatusRequest request)
    {
        _log.LogInformation("SetStatus project {ProjectId} → {IsActive}", projectId, request.IsActive);
        await _service.ChangeStatusAsync(projectId, request.IsActive, request.ModifiedBy);
        return NoContent();
    }
}
