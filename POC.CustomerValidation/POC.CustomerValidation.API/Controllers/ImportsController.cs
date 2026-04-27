using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

/// <summary>
/// Manages CSV/Excel file imports and the per-organisation column staging area.
///
/// Import lifecycle:
///   POST   /imports              → Upload file, auto-match headers (status: pending)
///   POST   /imports/{id}/mappings → Save column mappings (status: preview)
///   POST   /imports/{id}/preview  → Validate first 10 rows
///   POST   /imports/{id}/execute  → Run full import (status: importing → completed)
///
/// Unmatched headers are added to ImportColumnStaging for persistent resolution.
/// </summary>
[Route("api/organisations/{organisationId:guid}")]
[ApiController]
public class ImportsController(IImportService importService, IImportStagingService stagingService, ILogger<ImportsController> log) : ControllerBase
{
    private readonly IImportService        _importService  = importService;
    private readonly IImportStagingService _stagingService = stagingService;
    private readonly ILogger<ImportsController> _log       = log;

    // ---------------------------------------------------------------
    // Import Batches
    // ---------------------------------------------------------------

    /// <summary>
    /// Upload a CSV or Excel file. Parses headers, auto-matches columns against
    /// FieldDefinitions and customer fields, and creates an ImportBatch record.
    /// Unmatched headers are added to ImportColumnStaging automatically.
    /// </summary>
    [HttpPost("imports")]
    [EndpointSummary("Imports — upload file")]
    [ProducesResponseType(typeof(UploadImportResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload(
        Guid organisationId,
        IFormFile file,
        [FromForm] string uploadedBy        = "System",
        [FromForm] string duplicateStrategy = "skip")
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiError("BAD_REQUEST", "No file provided."));

        _log.LogInformation("Import upload for organisation {OrganisationId}: {FileName}", organisationId, file.FileName);
        var result = await _importService.UploadAsync(organisationId, file, uploadedBy, duplicateStrategy);
        return CreatedAtAction(nameof(GetBatch), new { organisationId, batchId = result.BatchId }, result);
    }

    /// <summary>List import history for the organisation, paginated.</summary>
    [HttpGet("imports")]
    [EndpointSummary("Imports — history")]
    [ProducesResponseType(typeof(PagedResult<ImportBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBatches(
        Guid organisationId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _importService.GetBatchesAsync(organisationId, page, pageSize);
        return Ok(result);
    }

    /// <summary>Get a single import batch by Id.</summary>
    [HttpGet("imports/{batchId:guid}")]
    [EndpointSummary("Imports — get batch")]
    [ProducesResponseType(typeof(ImportBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatch(Guid organisationId, Guid batchId)
    {
        var result = await _importService.GetBatchAsync(batchId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Check for saved column mappings for a given header fingerprint.
    /// Returns empty array if no saved mappings exist for this org + fingerprint combination.
    /// </summary>
    [HttpGet("imports/saved-mappings")]
    [EndpointSummary("Imports — get saved mappings")]
    [ProducesResponseType(typeof(IEnumerable<ColumnMatchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedMappings(Guid organisationId, [FromQuery] string fingerprint)
    {
        var result = await _importService.GetSavedMappingsAsync(organisationId, fingerprint);
        return Ok(result);
    }

    /// <summary>
    /// Save column mappings for a batch. Every column must be mapped or skipped.
    /// Advances batch status from 'pending' to 'preview'.
    /// </summary>
    [HttpPost("imports/{batchId:guid}/mappings")]
    [EndpointSummary("Imports — save mappings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveMappings(Guid organisationId, Guid batchId, [FromBody] SaveMappingsRequest request)
    {
        _log.LogInformation("Save mappings for batch {BatchId}", batchId);
        await _importService.SaveMappingsAsync(batchId, request);
        return NoContent();
    }

    /// <summary>
    /// Preview the first 10 data rows with the saved mapping applied.
    /// Returns per-row validation status (ok, warning, error) and summary counts.
    /// </summary>
    [HttpPost("imports/{batchId:guid}/preview")]
    [EndpointSummary("Imports — preview mapped data")]
    [ProducesResponseType(typeof(ImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview(Guid organisationId, Guid batchId)
    {
        _log.LogInformation("Preview batch {BatchId}", batchId);
        var result = await _importService.PreviewAsync(batchId);
        return Ok(result);
    }

    /// <summary>
    /// Execute the full import for a batch. Batch must be in 'preview' status.
    /// Returns 202 Accepted immediately. Poll GET /imports/{batchId} for completion status.
    /// </summary>
    [HttpPost("imports/{batchId:guid}/execute")]
    [EndpointSummary("Imports — execute import")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Execute(Guid organisationId, Guid batchId)
    {
        _log.LogInformation("Execute import batch {BatchId}", batchId);
        // Run in background — fire and forget; client polls status
        _ = Task.Run(() => _importService.ExecuteAsync(batchId));
        return Accepted();
    }

    /// <summary>Get the error rows for a completed import batch.</summary>
    [HttpGet("imports/{batchId:guid}/errors")]
    [EndpointSummary("Imports — get error rows")]
    [ProducesResponseType(typeof(IEnumerable<ImportErrorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetErrors(Guid organisationId, Guid batchId)
    {
        var result = await _importService.GetErrorsAsync(batchId);
        return Ok(result);
    }

    // ---------------------------------------------------------------
    // Import Column Staging
    // ---------------------------------------------------------------

    /// <summary>
    /// List staged (unresolved) column headers for the organisation.
    /// These are CSV/Excel headers that did not auto-match during a previous upload.
    /// </summary>
    [HttpGet("import-staging")]
    [EndpointSummary("Import staging — list")]
    [ProducesResponseType(typeof(IEnumerable<ImportColumnStagingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStaging(Guid organisationId, [FromQuery] string? status = null)
    {
        var result = await _stagingService.GetByOrganisationIdAsync(organisationId, status);
        return Ok(result);
    }

    /// <summary>Get a single staging record.</summary>
    [HttpGet("import-staging/{stagingId:guid}")]
    [EndpointSummary("Import staging — get by Id")]
    [ProducesResponseType(typeof(ImportColumnStagingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStagingById(Guid organisationId, Guid stagingId)
    {
        var result = await _stagingService.GetByIdAsync(stagingId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Resolve or skip a staged column. Once resolved, the mapping is applied automatically
    /// on the next upload containing the same header for this organisation.
    /// </summary>
    [HttpPut("import-staging/{stagingId:guid}")]
    [EndpointSummary("Import staging — resolve")]
    [ProducesResponseType(typeof(ImportColumnStagingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveStaging(Guid organisationId, Guid stagingId, [FromBody] ResolveColumnStagingRequest request)
    {
        _log.LogInformation("Resolve staging {StagingId} → {Status}", stagingId, request.Status);
        var result = await _stagingService.ResolveAsync(stagingId, request);
        return Ok(result);
    }

    /// <summary>
    /// Delete a staging record. The header will reappear as 'unmatched' on the next
    /// upload containing it. Use with caution.
    /// </summary>
    [HttpDelete("import-staging/{stagingId:guid}")]
    [EndpointSummary("Import staging — delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStaging(Guid organisationId, Guid stagingId)
    {
        _log.LogInformation("Delete staging record {StagingId}", stagingId);
        await _stagingService.DeleteAsync(stagingId);
        return NoContent();
    }
}
