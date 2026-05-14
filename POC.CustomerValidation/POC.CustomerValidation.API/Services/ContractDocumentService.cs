using System.Text.RegularExpressions;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class ContractDocumentService(
    IContractDocumentRepository repo,
    IContractRepository contractRepo,
    IOrganizationRepository orgRepo,
    IOrganizationStorageService storage,
    ILogger<ContractDocumentService> log) : IContractDocumentService
{
    static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/jpeg",
        "image/png"
    };

    // Virtual folder inside the org's blob container.
    const string Feature = "Contracts";

    public async Task<IEnumerable<ContractDocumentDto>> GetByContractIdAsync(Guid contractId)
    {
        var docs = await repo.GetByContractIdAsync(contractId);
        return docs.Select(Map);
    }

    public async Task<ContractDocumentDto?> GetByIdAsync(Guid documentId)
    {
        var doc = await repo.GetByIdAsync(documentId);
        return doc is null ? null : Map(doc);
    }

    public async Task<ContractDocumentDto> UploadAsync(Guid contractId, IFormFile file, UploadContractDocumentRequest request)
    {
        var contract = await contractRepo.GetByIdAsync(contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException(
                $"File type '{file.ContentType}' is not allowed. Accepted types: PDF, Word, JPEG, PNG.");

        var org = await orgRepo.GetByIdAsync(contract.OrganizationId)
            ?? throw new KeyNotFoundException($"Organisation {contract.OrganizationId} not found.");

        var containerName = storage.GetContainerName(org.OrganizationId, org.Abbreviation);
        var abbrevSlug    = Slugify(org.Abbreviation);
        var timestamp     = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        // Stored name: {abbrev}_{timestamp}_{original} — guarantees no collision even if same file re-uploaded.
        var blobName = $"{abbrevSlug}_{timestamp}_{SanitizeFileName(file.FileName)}";

        await using var stream = file.OpenReadStream();
        var blobPath = await storage.UploadFileAsync(containerName, Feature, blobName, stream, file.ContentType);

        log.LogInformation("Contract document uploaded: {OriginalFileName} → {Container}/{BlobPath}",
            file.FileName, containerName, blobPath);

        var document = new ContractDocument
        {
            DocumentId       = Guid.NewGuid(),
            ContractId       = contractId,
            AmendmentId      = request.AmendmentId,
            OriginalFileName = file.FileName,           // always the user-visible name
            StoredFileName   = blobName,                // renamed blob file
            StoragePath      = $"{containerName}/{blobPath}", // full reference: container + path
            ContentType      = file.ContentType,
            FileSizeBytes    = file.Length,
            UploadedAt       = DateTime.UtcNow,
            UploadedBy       = request.UploadedBy
        };

        var created = await repo.CreateAsync(document);
        return Map(created);
    }

    public async Task<(Stream FileStream, ContractDocumentDto Metadata)> DownloadAsync(Guid documentId)
    {
        var doc = await repo.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var (containerName, blobPath) = SplitStoragePath(doc.StoragePath);
        var stream = await storage.DownloadFileAsync(containerName, blobPath);
        return (stream, Map(doc));
    }

    public async Task DeleteAsync(Guid documentId)
    {
        var doc = await repo.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        await repo.DeleteAsync(documentId);

        var (containerName, blobPath) = SplitStoragePath(doc.StoragePath);
        await storage.DeleteFileAsync(containerName, blobPath);
        log.LogInformation("Contract document deleted: {StoragePath}", doc.StoragePath);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // StoragePath format: "{containerName}/{feature}/{blobName}"
    // e.g. "org-adx/Contracts/adx_20250508143022_contract.pdf"
    private static (string ContainerName, string BlobPath) SplitStoragePath(string storagePath)
    {
        var idx = storagePath.IndexOf('/');
        if (idx < 0)
            throw new InvalidOperationException($"Invalid storage path format: '{storagePath}'");
        return (storagePath[..idx], storagePath[(idx + 1)..]);
    }

    // Short, lowercase slug from the org abbreviation — used as a filename prefix.
    private static string Slugify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "org";
        var slug = Regex.Replace(text.ToLower().Trim(), @"[^a-z0-9]+", "");
        return slug.Length > 0 ? slug[..Math.Min(8, slug.Length)] : "org";
    }

    // Replace path separators and collapse whitespace so the blob name is URL-safe.
    private static string SanitizeFileName(string name)
        => Regex.Replace(name.Replace('/', '-').Replace('\\', '-'), @"\s+", "_");

    private static ContractDocumentDto Map(ContractDocument d) => new()
    {
        DocumentId       = d.DocumentId,
        ContractId       = d.ContractId,
        AmendmentId      = d.AmendmentId,
        OriginalFileName = d.OriginalFileName,  // user sees only this
        ContentType      = d.ContentType,
        FileSizeBytes    = d.FileSizeBytes,
        UploadedAt       = d.UploadedAt,
        UploadedBy       = d.UploadedBy
    };
}
