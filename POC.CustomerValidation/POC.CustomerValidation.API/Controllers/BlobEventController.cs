using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;

namespace POC.CustomerValidation.API.Controllers;

/// <summary>
/// Receives Azure Event Grid webhook notifications for blob storage events.
///
/// Setup in Azure Portal:
///   Storage account → Events → + Event Subscription
///     Endpoint type : Web Hook
///     Endpoint URL  : https://{your-api}/api/internal/blob-events
///     Event types   : Microsoft.Storage.BlobCreated
///     Filters       : Subject begins with /blobServices/default/containers/org-
///                     Subject ends with   .csv  (add xlsx, xls as needed)
/// </summary>
[Route("api/internal")]
[ApiController]
public class BlobEventController(
    IImportService importService,
    IOrganizationServices orgService,
    IOrganizationStorageService storageService,
    ILogger<BlobEventController> log) : ControllerBase
{
    [HttpPost("blob-events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleBlobEvent([FromBody] JsonElement[] events)
    {
        foreach (var evt in events)
        {
            if (!evt.TryGetProperty("eventType", out var typeProp)) continue;
            var eventType = typeProp.GetString();

            if (eventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                var validationCode = evt
                    .GetProperty("data")
                    .GetProperty("validationCode")
                    .GetString();
                log.LogInformation("Event Grid subscription validation handshake");
                return Ok(new { validationResponse = validationCode });
            }

            if (eventType == "Microsoft.Storage.BlobCreated")
            {
                var subject = evt.TryGetProperty("subject", out var s) ? s.GetString() : null;
                if (subject is not null)
                    await HandleBlobCreatedAsync(subject);
            }
        }

        return Ok();
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task HandleBlobCreatedAsync(string subject)
    {
        var (containerName, blobPath) = ParseSubject(subject);
        if (containerName is null || blobPath is null) return;

        // Only care about files dropped into an SFTP import folder
        if (!blobPath.StartsWith("imports/", StringComparison.OrdinalIgnoreCase)) return;
        if (blobPath.EndsWith(".keep", StringComparison.OrdinalIgnoreCase)) return;

        var ext = Path.GetExtension(blobPath).TrimStart('.').ToLowerInvariant();
        if (ext is not ("csv" or "xlsx" or "xls")) return;

        // imports/{projectId}/filename  →  segments[1] = projectId
        var segments = blobPath.Split('/');
        if (segments.Length < 3 || !int.TryParse(segments[1], out var projectId))
        {
            log.LogWarning("Blob path {BlobPath} does not match expected imports/{{projectId}}/file pattern", blobPath);
            return;
        }

        var org = await ResolveOrgByContainerAsync(containerName);
        if (org is null)
        {
            log.LogWarning("No organisation found for container {Container}", containerName);
            return;
        }

        var fileName = Path.GetFileName(blobPath);
        try
        {
            var batch = await importService.CreateBatchFromBlobAsync(
                org.OrganizationId, projectId, containerName, blobPath, fileName, "sftp");

            if (batch is not null)
                log.LogInformation("Registered SFTP upload {BlobPath} as import batch {BatchId}", blobPath, batch.BatchId);
        }
        catch (Exception ex)
        {
            // Log but don't rethrow — Event Grid retries on non-2xx, which would create duplicate batches
            log.LogError(ex, "Failed to register blob {BlobPath} as import batch", blobPath);
        }
    }

    private async Task<Models.DTOs.OrganizationDto?> ResolveOrgByContainerAsync(string containerName)
    {
        var orgs = await orgService.GetAllAsync();
        return orgs.FirstOrDefault(o =>
            storageService.GetContainerName(o.OrganizationId, o.Abbreviation) == containerName);
    }

    // subject: /blobServices/default/containers/{containerName}/blobs/{blobPath}
    private static (string? Container, string? BlobPath) ParseSubject(string subject)
    {
        const string blobsMarker = "/blobs/";
        var blobsIdx = subject.IndexOf(blobsMarker, StringComparison.OrdinalIgnoreCase);
        if (blobsIdx < 0) return (null, null);

        var containerPart = subject[..blobsIdx];
        var blobPath      = subject[(blobsIdx + blobsMarker.Length)..];

        var containerName = containerPart
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(blobPath)
            ? (null, null)
            : (containerName, blobPath);
    }
}
