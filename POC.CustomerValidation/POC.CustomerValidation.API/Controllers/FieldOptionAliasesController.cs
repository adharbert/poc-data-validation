using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

[Route("api/organisations/{organisationId:guid}/field-option-aliases")]
[ApiController]
public class FieldOptionAliasesController(
    IFieldOptionAliasService aliasService,
    ILogger<FieldOptionAliasesController> log) : ControllerBase
{
    private readonly IFieldOptionAliasService _aliasService = aliasService;
    private readonly ILogger<FieldOptionAliasesController> _log = log;

    /// <summary>List all value aliases for the organisation.</summary>
    [HttpGet]
    [EndpointSummary("Field option aliases — list")]
    [ProducesResponseType(typeof(IEnumerable<FieldOptionAliasDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid organisationId)
    {
        var result = await _aliasService.GetByOrganizationAsync(organisationId);
        return Ok(result);
    }

    /// <summary>
    /// Save (upsert) one or more aliases for the organisation.
    /// Existing aliases with the same OrganizationId + FieldDefinitionId + AliasValue are updated.
    /// </summary>
    [HttpPost("bulk")]
    [EndpointSummary("Field option aliases — bulk save")]
    [ProducesResponseType(typeof(IEnumerable<FieldOptionAliasDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkSave(Guid organisationId, [FromBody] BulkSaveAliasesRequest request)
    {
        if (request.Aliases.Count == 0)
            return BadRequest(new ApiError("BAD_REQUEST", "No aliases provided."));

        _log.LogInformation("Saving {Count} aliases for org {OrgId}", request.Aliases.Count, organisationId);
        var result = await _aliasService.BulkSaveAsync(organisationId, request);
        return Ok(result);
    }

    /// <summary>Delete a single alias by Id.</summary>
    [HttpDelete("{aliasId:guid}")]
    [EndpointSummary("Field option aliases — delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid organisationId, Guid aliasId)
    {
        _log.LogInformation("Delete alias {AliasId} for org {OrgId}", aliasId, organisationId);
        await _aliasService.DeleteAsync(aliasId);
        return NoContent();
    }
}
