using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FieldsController(IFieldDefinitionService services, ILogger<FieldsController> log) : ControllerBase
{
    private readonly IFieldDefinitionService _services = services;
    private readonly ILogger<FieldsController> _log = log;


    /// <summary>
    /// Get all field definitions per organization.
    /// </summary>
    /// <param name="organizationId"></param>
    /// <param name="includeInactive"></param>
    /// <returns>IEnumerable list of FieldDefinitionDto</returns>
    [HttpGet]
    [EndpointSummary("FieldDefinistions GET all for organization")]
    [ProducesResponseType(typeof(IEnumerable<FieldDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid organizationId, [FromQuery] bool includeInactive = false)
    {
        _log.LogInformation($"Get all field options for organization {organizationId}");
        var result = await _services.GetByOrganizationIdAsync(organizationId, includeInactive);
        return Ok(result);
    }


    /// <summary>
    /// Get field definition by Id.
    /// </summary>
    /// <param name="fieldId">Guid</param>
    /// <returns>FieldDefinitionDto</returns>
    [HttpGet("{fieldId:guid}")]
    [EndpointSummary("FieldDefinition GET by Id")]
    [ProducesResponseType(typeof(FieldDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid fieldId)
    {
        _log.LogInformation($"Get field option by Id: {fieldId}");
        var field = await _services.GetByIdAsync(fieldId);
        return field is null ? NotFound() : Ok(field);
    }



    /// <summary>
    /// Create field definition
    /// </summary>
    /// <param name="request">CreateFieldDefinitionRequest</param>
    /// <returns>FieldDefinitionDto</returns>
    [HttpPost]
    [EndpointSummary("FieldDefinition POST create new field")]
    [ProducesResponseType(typeof(FieldDefinitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFieldDefinitionRequest request)
    {
        _log.LogInformation($"Create new field defintion - FieldKey: {request.FieldKey}");
        var field = await _services.CreateAsync(request);
        return Ok(field);
    }



    /// <summary>
    /// Update field definition
    /// </summary>
    /// <param name="request">UpdateFieldDefinitionRequest</param>
    /// <returns>FieldDefinitionDto</returns>
    [HttpPut("{fieldId:guid}")]
    [EndpointSummary("FieldDefinition PUT update existing field")]
    [ProducesResponseType(typeof(FieldDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid fieldId, [FromBody] UpdateFieldDefinitionRequest request)
    {
        _log.LogInformation($"Update existing field definition. Id: {fieldId}");
        var field = await _services.UpdateAsync(request with { FieldDefinitionId = fieldId });
        return Ok(field);
    }



    /// <summary>
    /// Reorder field definition order for an organization.
    /// </summary>
    /// <param name="organizationId"></param>
    /// <param name="updates"></param>
    /// <returns></returns>
    [HttpPatch("organization/{organizationId:guid}/reorder")]
    [EndpointSummary("FieldDefinition PATCH bulk reorder by ogranization Id")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reorder(Guid organizationId, [FromBody] IEnumerable<ReorderFieldRequest> updates)
    {
        _log.LogInformation($"Reordering fields per organization Id.  Org Id: {organizationId}");
        await _services.ReorderAsync(organizationId, updates.Select(u => (u.FieldDefinitionId, u.DisplayOrder)));
        return NoContent();
    }



    /// <summary>
    /// Set status for field description.
    /// </summary>
    /// <param name="fieldDescriptionId"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpPatch("{fieldId:guid}/status/{status:bool}")]
    [EndpointSummary("FieldDefinition PATCH set field status.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetStatus(Guid fieldId, bool status)
    {
        _log.LogInformation($"Updating status for field Id: {fieldId} to status of {status}");
        await _services.SetStatusAsync(fieldId, status);
        return NoContent();
    }






}
