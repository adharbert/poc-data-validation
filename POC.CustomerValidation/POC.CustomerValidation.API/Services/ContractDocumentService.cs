using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class ContractDocumentService(
    IContractDocumentRepository repo,
    IContractRepository contractRepo,
    IConfiguration config,
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
            throw new InvalidOperationException(
                $"File type '{file.ContentType}' is not allowed. Accepted types: PDF, Word, JPEG, PNG.");

        var uploadRoot = config["ContractSettings:UploadPath"] ?? "uploads/contracts";
        var contractDir = Path.Combine(uploadRoot, contractId.ToString());
        Directory.CreateDirectory(contractDir);

        var extension   = Path.GetExtension(file.FileName);
        var storedName  = $"{Guid.NewGuid()}{extension}";
        var storagePath = Path.Combine(contractDir, storedName);

        await using (var fs = File.Create(storagePath))
            await file.CopyToAsync(fs);

        log.LogInformation("Contract document uploaded: {OriginalFileName} → {StoragePath}", file.FileName, storagePath);

        var document = new ContractDocument
        {
            DocumentId       = Guid.NewGuid(),
            ContractId       = contractId,
            AmendmentId      = request.AmendmentId,
            OriginalFileName = file.FileName,
            StoredFileName   = storedName,
            StoragePath      = storagePath,
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

        if (!File.Exists(doc.StoragePath))
        {
            log.LogError("Contract document file missing on disk: {StoragePath}", doc.StoragePath);
            throw new FileNotFoundException("Document file is no longer available on the server.", doc.StoragePath);
        }

        var stream = File.OpenRead(doc.StoragePath);
        return (stream, Map(doc));
    }

    public async Task DeleteAsync(Guid documentId)
    {
        var doc = await repo.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        await repo.DeleteAsync(documentId);

        if (File.Exists(doc.StoragePath))
        {
            File.Delete(doc.StoragePath);
            log.LogInformation("Contract document deleted from disk: {StoragePath}", doc.StoragePath);
        }
    }

    private static ContractDocumentDto Map(ContractDocument d) => new()
    {
        DocumentId       = d.DocumentId,
        ContractId       = d.ContractId,
        AmendmentId      = d.AmendmentId,
        OriginalFileName = d.OriginalFileName,
        ContentType      = d.ContentType,
        FileSizeBytes    = d.FileSizeBytes,
        UploadedAt       = d.UploadedAt,
        UploadedBy       = d.UploadedBy
    };
}
