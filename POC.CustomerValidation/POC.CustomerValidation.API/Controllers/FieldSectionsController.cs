using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

[ApiController]
[Route("api/organisations/{orgId:guid}")]
public class FieldSectionsController(IFieldSectionService sectionService, IFieldDefinitionService fieldService) : ControllerBase
{
    private readonly IFieldSectionService    _sectionService = sectionService;
    private readonly IFieldDefinitionService _fieldService   = fieldService;

    // ── Sections ─────────────────────────────────────────────────────────────

    [HttpGet("sections")]
    public async Task<IActionResult> GetSections(Guid orgId)
    {
        var sections = await _sectionService.GetByOrganizationIdAsync(orgId);
        return Ok(sections);
    }

    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection(Guid orgId, [FromBody] CreateFieldSectionRequest request)
    {
        var dto = await _sectionService.CreateAsync(orgId, request);
        return CreatedAtAction(nameof(GetSection), new { orgId, sectionId = dto.SectionId }, dto);
    }

    [HttpGet("sections/{sectionId:guid}")]
    public async Task<IActionResult> GetSection(Guid orgId, Guid sectionId)
    {
        var dto = await _sectionService.GetByIdAsync(sectionId);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("sections/{sectionId:guid}")]
    public async Task<IActionResult> UpdateSection(Guid orgId, Guid sectionId, [FromBody] UpdateFieldSectionRequest request)
    {
        var dto = await _sectionService.UpdateAsync(sectionId, request);
        return Ok(dto);
    }

    [HttpPatch("sections/{sectionId:guid}/status")]
    public async Task<IActionResult> SetSectionStatus(Guid orgId, Guid sectionId, [FromBody] SectionStatusRequest request)
    {
        await _sectionService.SetStatusAsync(sectionId, request.IsActive);
        return NoContent();
    }

    [HttpPost("sections/reorder")]
    public async Task<IActionResult> ReorderSections(Guid orgId, [FromBody] ReorderSectionsRequest request)
    {
        await _sectionService.ReorderAsync(request.Sections);
        return NoContent();
    }

    // ── Field assignment to section ───────────────────────────────────────────

    [HttpPut("sections/{sectionId:guid}/fields")]
    public async Task<IActionResult> AssignFields(Guid orgId, Guid sectionId, [FromBody] AssignFieldsToSectionRequest request)
    {
        await _sectionService.AssignFieldsAsync(sectionId, request);
        return NoContent();
    }

    // ── Form preview ─────────────────────────────────────────────────────────

    [HttpGet("customers/{customerId:guid}/form-preview")]
    public async Task<IActionResult> GetFormPreview(Guid orgId, Guid customerId)
    {
        var preview = await _fieldService.GetFormPreviewAsync(orgId, customerId);
        return Ok(preview);
    }
}

public record SectionStatusRequest(bool IsActive);
