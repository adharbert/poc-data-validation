using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

[Route("api/fields/{fieldId:guid}/options")]
[ApiController]
[Produces("application/json")]
public class FieldOptionsController(IFieldOptionService services, ILogger<FieldOptionsController> log) : ControllerBase
{
    private readonly IFieldOptionService _services = services;
    private readonly ILogger<FieldOptionsController> _log = log;


    /// <summary>
    /// Get all fieldOptions 
    /// </summary>
    /// <param name="fieldId"></param>
    /// <returns></returns>
    [HttpGet]
    [EndpointSummary("FieldOption GET all per field definition Id.")]
    [ProducesResponseType(typeof(IEnumerable<FieldOptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid fieldId)
    {
        var options = await _services.GetByFieldDefinitionIdAsync(fieldId);
        return Ok(options);
    }



    /// <summary>
    /// Create new field option.
    /// </summary>
    /// <param name="fieldId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [EndpointSummary("FieldOption POST create new option per field Id.")]
    [ProducesResponseType(typeof(FieldOptionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid fieldId, [FromBody] CreateFieldOptionRequest request)
    {
        _log.LogInformation($"Create new file option for file Id {fieldId}. Option key: {request.OptionKey}");
        var option = await _services.CreateAsync(fieldId, request);
        return StatusCode(StatusCodes.Status201Created, option);
    }




    /// <summary>
    /// Update existing field option.
    /// </summary>
    /// <param name="fieldId"></param>
    /// <param name="optionId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{optionId:guid}")]
    [EndpointSummary("FieldOption PUT update option per field Id")]
    [ProducesResponseType(typeof(FieldOptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid fieldId, Guid optionId, [FromBody] UpdateFieldOptionRequest request)
    {
        _log.LogInformation($"Update field option, for field Id: {fieldId} and option: {request.OptionKey}");
        var option = await _services.UpdateAsync(optionId, request);
        return Ok(option);
    }



    /// <summary>
    /// Bulk Update field options per field definition.
    /// </summary>
    /// <param name="fieldId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("bulk")]
    [EndpointSummary("FieldOption PUT bulk upsert option for many options.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkUpsert(Guid fieldId, [FromBody] BulkUpsertFieldOptionsRequest request)
    {
        _log.LogInformation($"Bulk upsert for field: {fieldId}.");
        await _services.BulkUpsertAsync(fieldId, request);
        return NoContent();
    }



    /// <summary>
    /// Delete FieldOption by Id.
    /// </summary>
    /// <param name="fieldId"></param>
    /// <param name="optionId"></param>
    /// <returns></returns>
    [HttpDelete("{optionId:guid}")]
    [EndpointSummary("FieldOption DELETE by field ")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid fieldId, Guid optionId)
    {
        _log.LogInformation($"deleting option for field Id: {fieldId} and option Id: {optionId}.");
        await _services.DeleteAsync(optionId);
        return NoContent();
    }


}
