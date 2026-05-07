using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using POC.CustomerValidation.API.Interfaces;

namespace POC.CustomerValidation.API.Services;

public class AzureBlobOrganizationStorageService(
    IConfiguration configuration,
    ILogger<AzureBlobOrganizationStorageService> logger) : IOrganizationStorageService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AzureBlobOrganizationStorageService> _logger = logger;

    public async Task ProvisionContainerAsync(Guid organizationId, string? abbreviation)
    {
        var connectionString = _configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

        var containerName = BuildContainerName(organizationId, abbreviation);

        try
        {
            var serviceClient = new BlobServiceClient(connectionString);
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            _logger.LogInformation("Provisioned Azure blob container {ContainerName} for org {OrgId}",
                containerName, organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision Azure blob container {ContainerName} for org {OrgId}",
                containerName, organizationId);
            throw;
        }
    }

    // Azure container names: 3–63 chars, lowercase letters, numbers, and hyphens only;
    // must start/end with letter or number.
    private static string BuildContainerName(Guid organizationId, string? abbreviation)
    {
        if (!string.IsNullOrWhiteSpace(abbreviation))
        {
            var slug = Regex.Replace(abbreviation.ToLower().Trim(), @"[^a-z0-9]+", "-").Trim('-');
            if (slug.Length >= 1)
                return $"org-{slug}"[..Math.Min(63, $"org-{slug}".Length)];
        }

        // Fallback: use first 12 hex chars of the org GUID (no hyphens)
        return $"org-{organizationId:N}"[..16];
    }
}
