using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using POC.CustomerValidation.API.Interfaces;

namespace POC.CustomerValidation.API.Services;

public class AzureBlobOrganizationStorageService(
    IConfiguration configuration,
    ILogger<AzureBlobOrganizationStorageService> logger) : IOrganizationStorageService
{
    public async Task ProvisionContainerAsync(Guid organisationId, string? abbreviation)
    {
        var containerClient = GetContainerClientByName(BuildContainerName(organisationId, abbreviation));
        try
        {
            await containerClient.CreateIfNotExistsAsync();
            logger.LogInformation("Provisioned blob container {Container} for org {OrgId}",
                containerClient.Name, organisationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision blob container {Container} for org {OrgId}",
                containerClient.Name, organisationId);
            throw;
        }
    }

    public string GetContainerName(Guid organisationId, string? abbreviation)
        => BuildContainerName(organisationId, abbreviation);

    /// <summary>
    /// Ensures the container exists, uploads the stream to {feature}/{blobName},
    /// and returns the blob path (e.g. "Contracts/ADX_20250508143022_doc.pdf").
    /// </summary>
    public async Task<string> UploadFileAsync(
        string containerName, string feature, string blobName,
        Stream content, string contentType)
    {
        var containerClient = GetContainerClientByName(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobPath   = $"{feature}/{blobName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        logger.LogInformation("Uploaded blob {BlobPath} to container {Container}", blobPath, containerName);
        return blobPath;
    }

    /// <summary>Opens a lazy read stream for the blob. Ensures the container exists first.</summary>
    public async Task<Stream> DownloadFileAsync(string containerName, string blobPath)
    {
        var containerClient = GetContainerClientByName(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobPath);
        return await blobClient.OpenReadAsync();
    }

    public async Task ProvisionProjectFolderAsync(string containerName, int projectId)
    {
        var containerClient = GetContainerClientByName(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var keepPath   = $"imports/{projectId}/.keep";
        var blobClient = containerClient.GetBlobClient(keepPath);

        await blobClient.UploadAsync(BinaryData.Empty, overwrite: true);
        logger.LogInformation("Provisioned SFTP folder imports/{ProjectId}/ in container {Container}",
            projectId, containerName);
    }

    public async IAsyncEnumerable<string> ListBlobsAsync(string containerName, string prefix)
    {
        var containerClient = GetContainerClientByName(containerName);
        if (!await containerClient.ExistsAsync()) yield break;
        await foreach (var item in containerClient.GetBlobsAsync(prefix: prefix))
            yield return item.Name;
    }

    /// <summary>Deletes the blob; silently no-ops if it does not exist.</summary>
    public async Task DeleteFileAsync(string containerName, string blobPath)
    {
        var containerClient = GetContainerClientByName(containerName);
        var blobClient      = containerClient.GetBlobClient(blobPath);
        var result          = await blobClient.DeleteIfExistsAsync();

        if (result.Value)
            logger.LogInformation("Deleted blob {BlobPath} from container {Container}", blobPath, containerName);
        else
            logger.LogWarning("Blob {BlobPath} not found in container {Container} — nothing deleted", blobPath, containerName);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private BlobContainerClient GetContainerClientByName(string containerName)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
        return new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
    }

    // Azure container names: 3–63 chars, lowercase letters/numbers/hyphens, start/end with letter or number.
    private static string BuildContainerName(Guid organisationId, string? abbreviation)
    {
        if (!string.IsNullOrWhiteSpace(abbreviation))
        {
            var slug = Regex.Replace(abbreviation.ToLower().Trim(), @"[^a-z0-9]+", "-").Trim('-');
            if (slug.Length >= 1)
            {
                var name = $"org-{slug}";
                return name[..Math.Min(63, name.Length)];
            }
        }
        return $"org-{organisationId:N}"[..16];
    }
}
