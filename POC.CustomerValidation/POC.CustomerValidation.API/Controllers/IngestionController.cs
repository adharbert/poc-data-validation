using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

/// <summary>
/// Automated ingestion pipeline for CSV / Excel files.
///
/// Flow:
///   POST   /ingestion/upload          → create IngestionJob (status: Pending)
///   GET    /ingestion                 → list jobs for the organisation
///   GET    /ingestion/{jobId}         → job status + row counts
///   GET    /ingestion/{jobId}/staging → staged rows (paginated, filterable)
///   POST   /ingestion/{jobId}/staging/{rowId}/review → approve or reject a row
///   POST   /ingestion/{jobId}/commit  → commit approved rows to customer tables
///
/// The background processor (IngestionProcessorJob) picks up Pending jobs,
/// normalises rows into IngestionStagingRows, and routes by tier:
///   Auto   → auto-committed immediately (no human needed)
///   Review → status becomes AwaitingReview; business user approves here
///   ETL    → status becomes AwaitingETL; ETL team takes over
/// </summary>
[Route("api/organisations/{organisationId:guid}/ingestion")]
[ApiController]
public class IngestionController(
    IIngestionJobService ingestionService,
    ILogger<IngestionController> log) : ControllerBase
{
    private readonly IIngestionJobService   _svc = ingestionService;
    private readonly ILogger<IngestionController> _log = log;

    // ---------------------------------------------------------------
    // Upload
    // ---------------------------------------------------------------

    /// <summary>
    /// Submit a CSV or Excel file for automated ingestion. Returns immediately
    /// with a job ID; the background processor handles the rest asynchronously.
    /// </summary>
    [HttpPost("upload")]
    [EndpointSummary("Ingestion — upload file")]
    [ProducesResponseType(typeof(IngestionJobDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload(
        Guid organisationId,
        IFormFile file,
        [FromForm] string uploadedBy = "System")
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiError("BAD_REQUEST", "No file provided."));

        _log.LogInformation("Ingestion upload for org {OrgId}: {FileName}", organisationId, file?.FileName);

        var job = await _svc.CreateJobAsync(organisationId, file!, uploadedBy);
        return AcceptedAtAction(nameof(GetJob), new { organisationId, jobId = job.Id }, job);
    }

    // ---------------------------------------------------------------
    // Job list / status
    // ---------------------------------------------------------------

    /// <summary>List ingestion jobs for the organisation, newest first.</summary>
    [HttpGet]
    [EndpointSummary("Ingestion — list jobs")]
    [ProducesResponseType(typeof(PagedResult<IngestionJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs(
        Guid organisationId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _svc.GetJobsAsync(organisationId, page, pageSize);
        return Ok(result);
    }

    /// <summary>Get status and row counts for a single ingestion job.</summary>
    [HttpGet("{jobId:guid}")]
    [EndpointSummary("Ingestion — job status")]
    [ProducesResponseType(typeof(IngestionJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJob(Guid organisationId, Guid jobId)
    {
        var job = await _svc.GetJobAsync(jobId);
        if (job is null || job.OrganizationId != organisationId)
            return NotFound(new ApiError("NOT_FOUND", $"Ingestion job {jobId} not found."));
        return Ok(job);
    }

    // ---------------------------------------------------------------
    // Staging rows
    // ---------------------------------------------------------------

    /// <summary>
    /// Return staged rows for a job. Filter by status (Pending, Pass, Flagged,
    /// Rejected, Committed) to focus on rows that need attention.
    /// </summary>
    [HttpGet("{jobId:guid}/staging")]
    [EndpointSummary("Ingestion — staging rows")]
    [ProducesResponseType(typeof(PagedResult<IngestionStagingRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStagingRows(
        Guid organisationId,
        Guid jobId,
        [FromQuery] string? status   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 50)
    {
        var job = await _svc.GetJobAsync(jobId);
        if (job is null || job.OrganizationId != organisationId)
            return NotFound(new ApiError("NOT_FOUND", $"Ingestion job {jobId} not found."));

        var rows = await _svc.GetStagingRowsAsync(jobId, status, page, pageSize);
        return Ok(rows);
    }

    // ---------------------------------------------------------------
    // Row review (approve / reject)
    // ---------------------------------------------------------------

    /// <summary>
    /// Approve or reject a single staging row. Body: { action: "approve"|"reject",
    /// reviewedBy: "name", reason: "optional" }
    /// </summary>
    [HttpPost("{jobId:guid}/staging/{rowId:guid}/review")]
    [EndpointSummary("Ingestion — review staging row")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewRow(
        Guid organisationId, Guid jobId, Guid rowId,
        [FromBody] ReviewStagingRowRequest request)
    {
        var job = await _svc.GetJobAsync(jobId);
        if (job is null || job.OrganizationId != organisationId)
            return NotFound(new ApiError("NOT_FOUND", $"Ingestion job {jobId} not found."));

        switch (request.Action?.ToLowerInvariant())
        {
            case "approve":
                await _svc.ApproveRowAsync(jobId, rowId, request.ReviewedBy);
                break;
            case "reject":
                await _svc.RejectRowAsync(jobId, rowId, request.ReviewedBy, request.Reason);
                break;
            default:
                return BadRequest(new ApiError("BAD_REQUEST", "Action must be 'approve' or 'reject'."));
        }

        return NoContent();
    }

    // ---------------------------------------------------------------
    // Commit
    // ---------------------------------------------------------------

    /// <summary>
    /// Commit all approved (Pass) staging rows to the customer tables.
    /// Only valid for jobs in AwaitingReview or AwaitingETL status.
    /// </summary>
    [HttpPost("{jobId:guid}/commit")]
    [EndpointSummary("Ingestion — commit job")]
    [ProducesResponseType(typeof(IngestionJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitJob(
        Guid organisationId, Guid jobId,
        [FromBody] CommitIngestionJobRequest? request)
    {
        var job = await _svc.GetJobAsync(jobId);
        if (job is null || job.OrganizationId != organisationId)
            return NotFound(new ApiError("NOT_FOUND", $"Ingestion job {jobId} not found."));

        await _svc.CommitJobAsync(jobId, request?.CommittedBy ?? "System");

        var updated = await _svc.GetJobAsync(jobId);
        return Ok(updated);
    }
}
