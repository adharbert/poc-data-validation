using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

[Route("api/library")]
[ApiController]
[Produces("application/json")]
public class LibraryController(ILibraryService service, ILogger<LibraryController> log) : ControllerBase
{
    private readonly ILibraryService            _service = service;
    private readonly ILogger<LibraryController> _log     = log;

    // -------------------------------------------------------
    // Sections
    // -------------------------------------------------------

    [HttpGet("sections")]
    [EndpointSummary("Get all library sections with their fields and options.")]
    [ProducesResponseType(typeof(IEnumerable<LibrarySectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSections([FromQuery] bool includeInactive = false)
    {
        var sections = await _service.GetAllSectionsAsync(includeInactive);
        return Ok(sections);
    }

    [HttpGet("sections/{sectionId:guid}")]
    [ProducesResponseType(typeof(LibrarySectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSection(Guid sectionId)
    {
        var section = await _service.GetSectionByIdAsync(sectionId);
        return section is null ? NotFound() : Ok(section);
    }

    [HttpPost("sections")]
    [ProducesResponseType(typeof(LibrarySectionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSection([FromBody] CreateLibrarySectionRequest request)
    {
        _log.LogInformation("Creating library section: {Name}", request.SectionName);
        var result = await _service.CreateSectionAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("sections/{sectionId:guid}")]
    [ProducesResponseType(typeof(LibrarySectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSection(Guid sectionId, [FromBody] UpdateLibrarySectionRequest request)
    {
        var result = await _service.UpdateSectionAsync(sectionId, request);
        return Ok(result);
    }

    [HttpPatch("sections/{sectionId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSectionStatus(Guid sectionId, [FromBody] SetStatusRequest request)
    {
        await _service.SetSectionStatusAsync(sectionId, request.IsActive);
        return NoContent();
    }

    [HttpPut("sections/{sectionId:guid}/fields")]
    [EndpointSummary("Replace the field assignments for a library section.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignFields(Guid sectionId, [FromBody] AssignLibraryFieldsRequest request)
    {
        await _service.AssignFieldsToSectionAsync(sectionId, request);
        return NoContent();
    }

    // -------------------------------------------------------
    // Fields
    // -------------------------------------------------------

    [HttpGet("fields")]
    [EndpointSummary("Get all library fields with their options.")]
    [ProducesResponseType(typeof(IEnumerable<LibraryFieldDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFields([FromQuery] bool includeInactive = false)
    {
        var fields = await _service.GetAllFieldsAsync(includeInactive);
        return Ok(fields);
    }

    [HttpGet("fields/{fieldId:guid}")]
    [ProducesResponseType(typeof(LibraryFieldDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetField(Guid fieldId)
    {
        var field = await _service.GetFieldByIdAsync(fieldId);
        return field is null ? NotFound() : Ok(field);
    }

    [HttpPost("fields")]
    [ProducesResponseType(typeof(LibraryFieldDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateField([FromBody] CreateLibraryFieldRequest request)
    {
        _log.LogInformation("Creating library field: {Key}", request.FieldKey);
        var result = await _service.CreateFieldAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("fields/{fieldId:guid}")]
    [ProducesResponseType(typeof(LibraryFieldDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateField(Guid fieldId, [FromBody] UpdateLibraryFieldRequest request)
    {
        var result = await _service.UpdateFieldAsync(fieldId, request);
        return Ok(result);
    }

    [HttpPatch("fields/{fieldId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetFieldStatus(Guid fieldId, [FromBody] SetStatusRequest request)
    {
        await _service.SetFieldStatusAsync(fieldId, request.IsActive);
        return NoContent();
    }

    [HttpPut("fields/{fieldId:guid}/options/bulk")]
    [EndpointSummary("Bulk upsert options for a library field.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkUpsertOptions(Guid fieldId, [FromBody] BulkUpsertFieldOptionsRequest request)
    {
        await _service.BulkUpsertOptionsAsync(fieldId, request);
        return NoContent();
    }

    // -------------------------------------------------------
    // Import to org
    // -------------------------------------------------------

    [HttpPost("import-to-org")]
    [EndpointSummary("Copy selected library sections (with fields and options) into an organisation.")]
    [ProducesResponseType(typeof(ImportFromLibraryResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportToOrg([FromBody] ImportFromLibraryRequest request)
    {
        _log.LogInformation("Importing {Count} library sections to org {OrgId}",
            request.SectionIds.Count(), request.OrganizationId);
        var result = await _service.ImportToOrgAsync(request);
        return Ok(result);
    }
}

