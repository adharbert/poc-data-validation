using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

/// <summary>
/// Handles job creation, status queries, staging row review, and final commit.
/// The heavy normalisation work (file parsing → staging rows) lives in
/// IngestionProcessorJob (BackgroundService).
/// </summary>
public class IngestionJobService(
    IIngestionRepository ingestionRepo,
    IOrganizationRepository orgRepo,
    ICustomerRepository customerRepo,
    ICustomerPhoneRepository phoneRepo,
    ICustomerEmailRepository emailRepo,
    ICustomerAddressRepository addressRepo,
    IFieldValueRepository fieldValueRepo,
    IConfiguration config,
    ILogger<IngestionJobService> log) : IIngestionJobService
{
    // ------------------------------------------------------------------
    // Upload — create an IngestionJob and save the file to disk.
    // The BackgroundService picks it up from Status = 'Pending'.
    // ------------------------------------------------------------------

    public async Task<IngestionJobDto> CreateJobAsync(Guid organizationId, IFormFile file, string uploadedBy)
    {
        _ = await orgRepo.GetByIdAsync(organizationId)
            ?? throw new KeyNotFoundException($"Organisation {organizationId} not found.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
        if (extension is not ("csv" or "xlsx" or "xls"))
            throw new ArgumentException($"Unsupported file type: {extension}");

        var uploadPath = config["ImportSettings:UploadPath"] ?? Path.Combine(Path.GetTempPath(), "imports");
        Directory.CreateDirectory(uploadPath);

        var jobId       = Guid.NewGuid();
        var storagePath = Path.Combine(uploadPath, $"ingest_{jobId}.{extension}");

        byte[] fileBytes;
        await using (var fs = File.Create(storagePath))
        {
            await file.CopyToAsync(fs);
            fileBytes = await File.ReadAllBytesAsync(storagePath);
        }

        var fileHash        = Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();
        var fingerprint     = ComputeFingerprint(file.FileName); // placeholder until headers parsed

        var job = new IngestionJob
        {
            Id                  = jobId,
            OrganizationId      = organizationId,
            FileName            = file.FileName,
            FileType            = extension,
            FileSizeBytes       = file.Length,
            FileHash            = fileHash,
            FileStoragePath     = storagePath,
            HeaderFingerprint   = fingerprint,
            UploadedBy          = uploadedBy,
            Status              = "Pending",
        };

        await ingestionRepo.CreateJobAsync(job);
        log.LogInformation("IngestionJob {JobId} created for org {OrgId}: {FileName}", jobId, organizationId, file.FileName);

        return MapJob(job);
    }

    // ------------------------------------------------------------------
    // Queries
    // ------------------------------------------------------------------

    public async Task<IngestionJobDto?> GetJobAsync(Guid jobId)
    {
        var job = await ingestionRepo.GetJobByIdAsync(jobId);
        return job is null ? null : MapJob(job);
    }

    public async Task<PagedResult<IngestionJobDto>> GetJobsAsync(Guid organizationId, int page, int pageSize)
    {
        var (items, total) = await ingestionRepo.GetJobsByOrganizationAsync(organizationId, page, pageSize);
        return new PagedResult<IngestionJobDto>(items.Select(MapJob), total, page, pageSize);
    }

    public async Task<PagedResult<IngestionStagingRowDto>> GetStagingRowsAsync(
        Guid jobId, string? statusFilter, int page, int pageSize)
    {
        var (items, total) = await ingestionRepo.GetStagingRowsAsync(jobId, statusFilter, page, pageSize);
        return new PagedResult<IngestionStagingRowDto>(items.Select(MapStagingRow), total, page, pageSize);
    }

    // ------------------------------------------------------------------
    // Row review
    // ------------------------------------------------------------------

    public async Task ApproveRowAsync(Guid jobId, Guid rowId, string reviewedBy)
    {
        var (items, _) = await ingestionRepo.GetStagingRowsAsync(jobId, null, 1, int.MaxValue);
        var row = items.FirstOrDefault(r => r.Id == rowId)
            ?? throw new KeyNotFoundException($"Staging row {rowId} not found.");

        row.Status     = "Pass";
        row.ReviewedBy = reviewedBy;
        row.ReviewedAt = DateTime.UtcNow;
        await ingestionRepo.UpdateStagingRowAsync(row);
    }

    public async Task RejectRowAsync(Guid jobId, Guid rowId, string reviewedBy, string? reason)
    {
        var (items, _) = await ingestionRepo.GetStagingRowsAsync(jobId, null, 1, int.MaxValue);
        var row = items.FirstOrDefault(r => r.Id == rowId)
            ?? throw new KeyNotFoundException($"Staging row {rowId} not found.");

        row.Status      = "Rejected";
        row.ReviewedBy  = reviewedBy;
        row.ReviewedAt  = DateTime.UtcNow;
        if (reason is not null)
            row.FlagReasons = JsonSerializer.Serialize(new[] { reason });

        await ingestionRepo.UpdateStagingRowAsync(row);
    }

    // ------------------------------------------------------------------
    // Commit — write approved staging rows to customer tables
    // ------------------------------------------------------------------

    public async Task CommitJobAsync(Guid jobId, string committedBy)
    {
        var job = await ingestionRepo.GetJobByIdAsync(jobId)
            ?? throw new KeyNotFoundException($"IngestionJob {jobId} not found.");

        if (job.Status is not ("AwaitingReview" or "AwaitingETL" or "Processing"))
            throw new InvalidOperationException($"Job is not in a committable state. Current status: {job.Status}");

        var org = await orgRepo.GetByIdAsync(job.OrganizationId)
            ?? throw new InvalidOperationException($"Organisation {job.OrganizationId} not found.");

        job.Status = "Committing";
        await ingestionRepo.UpdateJobAsync(job);

        var stagingRows = (await ingestionRepo.GetCommittableStagingRowsAsync(jobId)).ToList();
        var abbreviation = (org.Abbreviation ?? org.OrganizationCode[..Math.Min(6, org.OrganizationCode.Length)]).ToUpperInvariant().Trim();

        int committed = 0, failed = 0;
        var committedIds = new List<Guid>();

        foreach (var row in stagingRows)
        {
            try
            {
                await CommitStagingRowAsync(row, job.OrganizationId, abbreviation);
                committedIds.Add(row.Id);
                committed++;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to commit staging row {RowId} (row #{RowNumber})", row.Id, row.RowNumber);
                row.Status      = "Flagged";
                row.FlagReasons = JsonSerializer.Serialize(new[] { ex.Message });
                await ingestionRepo.UpdateStagingRowAsync(row);
                failed++;
            }
        }

        if (committedIds.Count > 0)
            await ingestionRepo.MarkStagingRowsCommittedAsync(committedIds);

        job.Status      = "Complete";
        job.PassedRows  = committed;
        job.FailedRows  = failed;
        job.CompletedAt = DateTime.UtcNow;
        await ingestionRepo.UpdateJobAsync(job);

        log.LogInformation("IngestionJob {JobId} committed: {Committed} rows written, {Failed} failed", jobId, committed, failed);
    }

    // ------------------------------------------------------------------
    // Private: commit a single staging row to customer tables
    // ------------------------------------------------------------------

    private async Task CommitStagingRowAsync(IngestionStagingRow row, Guid organizationId, string abbreviation)
    {
        var data = JsonSerializer.Deserialize<StagingRowData>(row.RowJson, JsonOptions)
            ?? throw new InvalidOperationException("Could not deserialise RowJson.");

        // Deduplication by Email
        Customer? existing = null;
        if (!string.IsNullOrWhiteSpace(data.Customer?.Email))
            existing = await customerRepo.GetByEmailAsync(organizationId, data.Customer.Email);

        if (existing is null)
        {
            var code = GenerateCustomerCode(abbreviation);
            var customer = new Customer
            {
                OrganizationId  = organizationId,
                FirstName       = data.Customer?.FirstName ?? throw new InvalidOperationException("FirstName is required."),
                LastName        = data.Customer?.LastName  ?? throw new InvalidOperationException("LastName is required."),
                MiddleName      = data.Customer?.MiddleName,
                MaidenName      = data.Customer?.MaidenName,
                DateOfBirth     = data.Customer?.DateOfBirth,
                OriginalId      = data.Customer?.OriginalId,
                Email           = data.Customer?.Email,
                Phone           = data.Customer?.Phone,
                CustomerCode    = code,
                IsActive        = true,
            };
            existing = await customerRepo.CreateAsync(customer);

            if (!string.IsNullOrWhiteSpace(data.Customer?.Phone))
                await phoneRepo.CreateAsync(new CustomerPhone
                {
                    CustomerId  = existing.CustomerId,
                    PhoneNumber = data.Customer.Phone,
                    PhoneType   = "mobile",
                    IsPrimary   = true,
                });

            if (!string.IsNullOrWhiteSpace(data.Customer?.Email))
                await emailRepo.CreateAsync(new CustomerEmail
                {
                    CustomerId   = existing.CustomerId,
                    EmailAddress = data.Customer.Email,
                    EmailType    = "personal",
                    IsPrimary    = true,
                });

            if (data.Address is not null &&
                !string.IsNullOrWhiteSpace(data.Address.AddressLine1) &&
                !string.IsNullOrWhiteSpace(data.Address.City))
            {
                await addressRepo.CreateAsync(new CustomerAddress
                {
                    CustomerId   = existing.CustomerId,
                    AddressLine1 = data.Address.AddressLine1!,
                    AddressLine2 = data.Address.AddressLine2,
                    City         = data.Address.City!,
                    State        = data.Address.State ?? string.Empty,
                    PostalCode   = data.Address.PostalCode ?? string.Empty,
                    Country      = data.Address.Country ?? "US",
                    AddressType  = data.Address.AddressType ?? "primary",
                    IsCurrent    = true,
                });
            }
        }

        foreach (var fv in data.FieldValues ?? [])
        {
            await fieldValueRepo.UpsertAsync(new FieldValue
            {
                CustomerId          = existing.CustomerId,
                FieldDefinitionId   = fv.FieldDefinitionId,
                ValueText           = fv.Value,
                CreatedDt           = DateTime.UtcNow,
                ModifiedDt          = DateTime.UtcNow,
            });
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string GenerateCustomerCode(string abbreviation)
        => $"{abbreviation}-{Ulid.NewUlid().ToString()[..10]}";

    private static string ComputeFingerprint(string fileName)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fileName.ToLowerInvariant()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static IngestionJobDto MapJob(IngestionJob j) => new()
    {
        Id              = j.Id,
        OrganizationId  = j.OrganizationId,
        FileName        = j.FileName,
        FileType        = j.FileType,
        FileSizeBytes   = j.FileSizeBytes,
        UploadedBy      = j.UploadedBy,
        UploadedAt      = j.UploadedAt,
        Status          = j.Status,
        Tier            = j.Tier,
        TotalRows       = j.TotalRows,
        PassedRows      = j.PassedRows,
        FlaggedRows     = j.FlaggedRows,
        FailedRows      = j.FailedRows,
        ErrorMessage    = j.ErrorMessage,
        CompletedAt     = j.CompletedAt,
    };

    private static IngestionStagingRowDto MapStagingRow(IngestionStagingRow r) => new()
    {
        Id              = r.Id,
        IngestionJobId  = r.IngestionJobId,
        RowNumber       = r.RowNumber,
        RowJson         = r.RowJson,
        ConfidenceScore = r.ConfidenceScore,
        Status          = r.Status,
        FlagReasons     = r.FlagReasons,
        ReviewedBy      = r.ReviewedBy,
        ReviewedAt      = r.ReviewedAt,
    };

    // ------------------------------------------------------------------
    // Internal DTO for deserialising RowJson
    // ------------------------------------------------------------------

    private record StagingRowData(
        StagingCustomerData?            Customer,
        StagingAddressData?             Address,
        List<StagingFieldValue>?        FieldValues);

    private record StagingCustomerData(
        string?     FirstName,
        string?     LastName,
        string?     MiddleName,
        string?     MaidenName,
        DateOnly?   DateOfBirth,
        string?     Email,
        string?     Phone,
        string?     OriginalId);

    private record StagingAddressData(
        string?     AddressLine1,
        string?     AddressLine2,
        string?     City,
        string?     State,
        string?     PostalCode,
        string?     Country,
        string?     AddressType);

    private record StagingFieldValue(Guid FieldDefinitionId, string? Value);
}
