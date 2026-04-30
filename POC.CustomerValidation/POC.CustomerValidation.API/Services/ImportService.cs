using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class ImportService(
    IImportRepository importRepo,
    IImportColumnStagingRepository stagingRepo,
    IFieldDefinitionRepository fieldRepo,
    IOrganizationRepository orgRepo,
    ICustomerRepository customerRepo,
    IFieldValueRepository fieldValueRepo,
    IConfiguration config,
    ILogger<ImportService> log) : IImportService
{
    private static readonly string[] CustomerFields = ["FirstName", "LastName", "MiddleName", "Email", "OriginalId"];

    public async Task<UploadImportResponseDto> UploadAsync(
        Guid organisationId, IFormFile file, string uploadedBy, string duplicateStrategy = "skip")
    {
        var org = await orgRepo.GetByIdAsync(organisationId)
            ?? throw new KeyNotFoundException($"Organisation {organisationId} not found.");

        var extension   = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
        var fileType    = extension is "xlsx" or "xls" or "csv" ? extension : throw new ArgumentException($"Unsupported file type: {extension}");

        var uploadPath  = config["ImportSettings:UploadPath"] ?? Path.Combine(Path.GetTempPath(), "imports");
        Directory.CreateDirectory(uploadPath);

        // Parse headers + row count from the file
        (string[] headers, int rowCount) = fileType == "csv"
            ? ParseCsvHeaders(file)
            : ParseXlsxHeaders(file);

        var fingerprint     = ComputeFingerprint(headers);
        var batchId         = Guid.NewGuid();
        var storagePath     = Path.Combine(uploadPath, $"{batchId}.{extension}");

        // Persist the uploaded file for use during preview and execute
        await using (var fs = File.Create(storagePath))
            await file.CopyToAsync(fs);

        var batch = new ImportBatch
        {
            BatchId             = batchId,
            OrganizationId      = organisationId,
            FileName            = file.FileName,
            FileType            = fileType,
            FileHeaders         = JsonSerializer.Serialize(headers),
            HeaderFingerprint   = fingerprint,
            FileStoragePath     = storagePath,
            TotalRows           = rowCount,
            Status              = "pending",
            DuplicateStrategy   = duplicateStrategy,
            UploadedBy          = uploadedBy,
        };
        await importRepo.CreateBatchAsync(batch);

        // Check for saved mappings from a previous upload with identical headers
        var savedMappings   = (await importRepo.GetSavedMappingsAsync(organisationId, fingerprint)).ToList();
        var hasSaved        = savedMappings.Count > 0;

        // Auto-match each column
        var fieldDefs   = (await fieldRepo.GetByOrganizationIdAsync(organisationId)).ToList();
        var matches     = await AutoMatchHeaders(headers, fieldDefs, savedMappings, organisationId);

        // Add unmatched headers to ImportColumnStaging
        foreach (var match in matches.Where(m => m.MatchStatus == "unmatched"))
        {
            var norm     = match.CsvHeader.Trim().ToLowerInvariant();
            var existing = await stagingRepo.GetByHeaderAsync(organisationId, norm);
            if (existing is null)
                await stagingRepo.CreateAsync(new ImportColumnStaging
                {
                    OrganizationId      = organisationId,
                    CsvHeader           = match.CsvHeader,
                    HeaderNormalized    = norm,
                    Status              = "unmatched",
                });
            else
                await stagingRepo.TouchAsync(existing.StagingId);
        }

        return new UploadImportResponseDto
        {
            BatchId         = batch.BatchId,
            Headers         = headers,
            RowCount        = rowCount,
            HasSavedMappings = hasSaved,
            ColumnMatches   = matches,
        };
    }

    public async Task<IEnumerable<ColumnMatchResultDto>> GetSavedMappingsAsync(Guid organisationId, string fingerprint)
    {
        var saved       = await importRepo.GetSavedMappingsAsync(organisationId, fingerprint);
        var fieldDefs   = (await fieldRepo.GetByOrganizationIdAsync(organisationId)).ToList();

        return saved.Select(s =>
        {
            var fd = s.FieldDefinitionId.HasValue
                ? fieldDefs.FirstOrDefault(f => f.FieldDefinitionId == s.FieldDefinitionId.Value)
                : null;

            return new ColumnMatchResultDto
            {
                ColumnIndex         = s.CsvColumnIndex,
                CsvHeader           = s.CsvHeader,
                MatchStatus         = "matched",
                MappingType         = s.MappingType,
                CustomerFieldName   = s.CustomerFieldName,
                FieldDefinitionId   = s.FieldDefinitionId,
                FieldLabel          = fd?.FieldLabel,
                IsAutoMatched       = false,
            };
        });
    }

    public async Task SaveMappingsAsync(Guid batchId, SaveMappingsRequest request)
    {
        var batch = await importRepo.GetBatchByIdAsync(batchId)
            ?? throw new KeyNotFoundException($"Import batch {batchId} not found.");

        var mappings = request.Mappings.Select((m, i) => new ImportColumnMapping
        {
            ImportBatchId       = batchId,
            CsvHeader           = m.CsvHeader,
            CsvColumnIndex      = m.ColumnIndex,
            MappingType         = m.MappingType,
            CustomerFieldName   = m.CustomerFieldName,
            FieldDefinitionId   = m.FieldDefinitionId,
            IsAutoMatched       = false,
            SavedForReuse       = m.SaveForReuse,
            DisplayOrder        = i,
        }).ToList();

        await importRepo.SaveMappingsAsync(batchId, mappings);

        batch.Status            = "preview";
        batch.DuplicateStrategy = request.DuplicateStrategy;
        batch.MappingSavedAt    = DateTime.UtcNow;
        await importRepo.UpdateBatchAsync(batch);
    }

    public async Task<ImportPreviewDto> PreviewAsync(Guid batchId)
    {
        var batch = await importRepo.GetBatchByIdAsync(batchId)
            ?? throw new KeyNotFoundException($"Import batch {batchId} not found.");

        var mappings    = (await importRepo.GetMappingsByBatchIdAsync(batchId)).ToList();
        var headers     = JsonSerializer.Deserialize<string[]>(batch.FileHeaders) ?? [];
        var rows        = ReadFileRows(batch.FileStoragePath!, batch.FileType!, maxRows: 10);
        var fieldDefs   = await GetFieldDefsForBatch(mappings);

        var previewRows = new List<PreviewRowDto>();
        int ok = 0, warn = 0, err = 0;

        foreach (var (row, idx) in rows.Select((r, i) => (r, i + 1)))
        {
            var (status, message) = ValidateRow(row, mappings, fieldDefs);
            previewRows.Add(new PreviewRowDto { RowNumber = idx, Values = row, Status = status, Message = message });
            switch (status) { case "ok": ok++;  break; case "warning": warn++; break; default: err++; break; }
        }

        return new ImportPreviewDto
        {
            Headers         = headers,
            Rows            = previewRows,
            OkCount         = ok,
            WarningCount    = warn,
            ErrorCount      = err,
        };
    }

    public async Task ExecuteAsync(Guid batchId)
    {
        var batch = await importRepo.GetBatchByIdAsync(batchId)
            ?? throw new KeyNotFoundException($"Import batch {batchId} not found.");

        if (batch.Status != "preview")
            throw new InvalidOperationException($"Batch must be in 'preview' status to execute. Current status: {batch.Status}");

        batch.Status                = "importing";
        batch.ExecutionStartedAt    = DateTime.UtcNow;
        await importRepo.UpdateBatchAsync(batch);

        var org = await orgRepo.GetByIdAsync(batch.OrganizationId)!;
        var mappings    = (await importRepo.GetMappingsByBatchIdAsync(batchId)).ToList();
        var fieldDefs   = await GetFieldDefsForBatch(mappings);
        var allRows     = ReadFileRows(batch.FileStoragePath!, batch.FileType!);

        var abbreviation    = (org!.Abbreviation ?? org.OrganizationCode[..Math.Min(4, org.OrganizationCode.Length)]).ToUpperInvariant().Trim();
        int imported = 0, skipped = 0, errors = 0;

        foreach (var (row, rowNum) in allRows.Select((r, i) => (r, i + 1)))
        {
            try
            {
                var customerData    = ExtractCustomerFields(row, mappings);
                var fieldValues     = ExtractFieldValues(row, mappings);

                // Deduplication by Email
                Customer? existing = null;
                if (!string.IsNullOrWhiteSpace(customerData.Email))
                    existing = await customerRepo.GetByEmailAsync(batch.OrganizationId, customerData.Email);

                if (existing is not null && batch.DuplicateStrategy == "skip")
                {
                    skipped++;
                    continue;
                }
                if (existing is not null && batch.DuplicateStrategy == "error")
                {
                    await importRepo.AddErrorAsync(new ImportError
                    {
                        ImportBatchId   = batchId,
                        RowNumber       = rowNum,
                        RawData         = JsonSerializer.Serialize(row),
                        ErrorType       = "duplicate",
                        ErrorMessage    = $"Customer with email '{customerData.Email}' already exists.",
                    });
                    errors++;
                    continue;
                }

                if (existing is null)
                {
                    var code        = GenerateCustomerCode(abbreviation);
                    var customer    = new Customer
                    {
                        OrganizationId  = batch.OrganizationId,
                        FirstName       = customerData.FirstName ?? throw new InvalidOperationException("FirstName is required."),
                        LastName        = customerData.LastName ?? throw new InvalidOperationException("LastName is required."),
                        MiddleName      = customerData.MiddleName,
                        OriginalId      = customerData.OriginalId,
                        Email           = customerData.Email,
                        CustomerCode    = code,
                        IsActive        = true,
                    };
                    existing = await customerRepo.CreateAsync(customer);
                }
                else if (batch.DuplicateStrategy == "update")
                {
                    existing.FirstName  = customerData.FirstName ?? existing.FirstName;
                    existing.LastName   = customerData.LastName  ?? existing.LastName;
                    existing.MiddleName = customerData.MiddleName ?? existing.MiddleName;
                    existing.OriginalId = customerData.OriginalId ?? existing.OriginalId;
                    await customerRepo.UpdateAsync(existing);
                }

                foreach (var (fieldDefId, value) in fieldValues)
                {
                    var fieldDef = fieldDefs.GetValueOrDefault(fieldDefId);
                    var storedValue = fieldDef?.FieldType == "phone"
                        ? Regex.Replace(value ?? string.Empty, @"\D", "")
                        : value;

                    await fieldValueRepo.UpsertAsync(new FieldValue
                    {
                        CustomerId          = existing.CustomerId,
                        FieldDefinitionId   = fieldDefId,
                        ValueText           = storedValue,
                        CreatedDt           = DateTime.UtcNow,
                        ModifiedDt          = DateTime.UtcNow,
                    });
                }

                imported++;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Import row {RowNumber} failed for batch {BatchId}", rowNum, batchId);
                await importRepo.AddErrorAsync(new ImportError
                {
                    ImportBatchId   = batchId,
                    RowNumber       = rowNum,
                    RawData         = JsonSerializer.Serialize(row),
                    ErrorType       = "system",
                    ErrorMessage    = ex.Message,
                });
                errors++;
            }
        }

        // Save successful mappings for reuse
        var reuseMappings = mappings.Where(m => m.SavedForReuse).Select(m => new SavedColumnMapping
        {
            OrganizationId      = batch.OrganizationId,
            HeaderFingerprint   = batch.HeaderFingerprint,
            CsvHeader           = m.CsvHeader,
            CsvColumnIndex      = m.CsvColumnIndex,
            MappingType         = m.MappingType,
            CustomerFieldName   = m.CustomerFieldName,
            FieldDefinitionId   = m.FieldDefinitionId,
            DisplayOrder        = m.DisplayOrder,
        }).ToList();

        if (reuseMappings.Count > 0)
            await importRepo.SaveColumnMappingsAsync(batch.OrganizationId, batch.HeaderFingerprint, reuseMappings);

        batch.Status        = "completed";
        batch.ImportedRows  = imported;
        batch.SkippedRows   = skipped;
        batch.ErrorRows     = errors;
        batch.CompletedAt   = DateTime.UtcNow;
        await importRepo.UpdateBatchAsync(batch);

        log.LogInformation("Import batch {BatchId} completed: {Imported} imported, {Skipped} skipped, {Errors} errors", batchId, imported, skipped, errors);
    }

    public async Task<PagedResult<ImportBatchDto>> GetBatchesAsync(Guid organisationId, int page = 1, int pageSize = 20)
    {
        var (items, total) = await importRepo.GetBatchesByOrganisationAsync(organisationId, page, pageSize);
        return new PagedResult<ImportBatchDto>(items.Select(MapBatch), total, page, pageSize);
    }

    public async Task<ImportBatchDto?> GetBatchAsync(Guid batchId)
    {
        var batch = await importRepo.GetBatchByIdAsync(batchId);
        return batch is null ? null : MapBatch(batch);
    }

    public async Task<IEnumerable<ImportErrorDto>> GetErrorsAsync(Guid batchId)
    {
        var errors = await importRepo.GetErrorsByBatchIdAsync(batchId);
        return errors.Select(e => new ImportErrorDto
        {
            ErrorId         = e.ErrorId,
            RowNumber       = e.RowNumber,
            ErrorType       = e.ErrorType,
            ErrorMessage    = e.ErrorMessage,
            RawData         = e.RawData,
        });
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private static (string[] Headers, int RowCount) ParseCsvHeaders(IFormFile file)
    {
        using var reader    = new StreamReader(file.OpenReadStream());
        using var csv       = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord!;
        int rows    = 0;
        while (csv.Read()) rows++;
        return (headers, rows);
    }

    private static (string[] Headers, int RowCount) ParseXlsxHeaders(IFormFile file)
    {
        using var wb    = new XLWorkbook(file.OpenReadStream());
        var ws          = wb.Worksheets.First();
        var headerRow   = ws.FirstRowUsed();
        if (headerRow is null) return ([], 0);
        var headers     = headerRow.CellsUsed().Select(c => c.GetString()).ToArray();
        var rowCount    = ws.RowsUsed().Count() - 1;
        return (headers, Math.Max(0, rowCount));
    }

    private static IEnumerable<string?[]> ReadFileRows(string path, string fileType, int maxRows = int.MaxValue)
    {
        if (!File.Exists(path)) yield break;

        if (fileType == "csv")
        {
            using var reader    = new StreamReader(path);
            using var csv       = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Read();
            csv.ReadHeader();
            int count = 0;
            while (csv.Read() && count < maxRows)
            {
                var record = new string?[csv.HeaderRecord!.Length];
                for (int i = 0; i < record.Length; i++)
                    record[i] = csv.GetField(i);
                yield return record;
                count++;
            }
        }
        else
        {
            using var wb    = new XLWorkbook(path);
            var ws          = wb.Worksheets.First();
            var colCount    = ws.FirstRowUsed()?.CellsUsed().Count() ?? 0;
            int count       = 0;
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                if (count >= maxRows) break;
                yield return Enumerable.Range(1, colCount).Select(i => (string?)row.Cell(i).GetString()).ToArray();
                count++;
            }
        }
    }

    private async Task<List<ColumnMatchResultDto>> AutoMatchHeaders(
        string[] headers, List<FieldDefinition> fieldDefs,
        List<SavedColumnMapping> savedMappings, Guid organisationId)
    {
        var result = new List<ColumnMatchResultDto>();

        for (int i = 0; i < headers.Length; i++)
        {
            var header  = headers[i];
            var trimmed = header.Trim();

            // Saved mapping takes priority
            var saved = savedMappings.FirstOrDefault(s => s.CsvHeader.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (saved is not null)
            {
                var fd = saved.FieldDefinitionId.HasValue
                    ? fieldDefs.FirstOrDefault(f => f.FieldDefinitionId == saved.FieldDefinitionId.Value)
                    : null;
                result.Add(new ColumnMatchResultDto
                {
                    ColumnIndex         = i,
                    CsvHeader           = header,
                    MatchStatus         = "matched",
                    MappingType         = saved.MappingType,
                    CustomerFieldName   = saved.CustomerFieldName,
                    FieldDefinitionId   = saved.FieldDefinitionId,
                    FieldLabel          = fd?.FieldLabel,
                    IsAutoMatched       = false,
                });
                continue;
            }

            // Customer field match (case-insensitive)
            var custField = CustomerFields.FirstOrDefault(f => f.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (custField is not null)
            {
                result.Add(new ColumnMatchResultDto
                {
                    ColumnIndex         = i,
                    CsvHeader           = header,
                    MatchStatus         = "matched",
                    MappingType         = "customer_field",
                    CustomerFieldName   = custField,
                    IsAutoMatched       = true,
                });
                continue;
            }

            // FieldKey match (case-insensitive)
            var byKey = fieldDefs.FirstOrDefault(f => f.FieldKey.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (byKey is not null)
            {
                result.Add(new ColumnMatchResultDto
                {
                    ColumnIndex         = i,
                    CsvHeader           = header,
                    MatchStatus         = "matched",
                    MappingType         = "field_definition",
                    FieldDefinitionId   = byKey.FieldDefinitionId,
                    FieldLabel          = byKey.FieldLabel,
                    IsAutoMatched       = true,
                });
                continue;
            }

            // FieldLabel match (case-insensitive)
            var byLabel = fieldDefs.FirstOrDefault(f => f.FieldLabel.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (byLabel is not null)
            {
                result.Add(new ColumnMatchResultDto
                {
                    ColumnIndex         = i,
                    CsvHeader           = header,
                    MatchStatus         = "matched",
                    MappingType         = "field_definition",
                    FieldDefinitionId   = byLabel.FieldDefinitionId,
                    FieldLabel          = byLabel.FieldLabel,
                    IsAutoMatched       = true,
                });
                continue;
            }

            // Check staging for a previously resolved mapping
            var norm    = trimmed.ToLowerInvariant();
            var staging = await stagingRepo.GetByHeaderAsync(organisationId, norm);
            if (staging?.Status == "resolved" && staging.MappingType is not null)
            {
                var fd = staging.FieldDefinitionId.HasValue
                    ? fieldDefs.FirstOrDefault(f => f.FieldDefinitionId == staging.FieldDefinitionId.Value)
                    : null;
                result.Add(new ColumnMatchResultDto
                {
                    ColumnIndex         = i,
                    CsvHeader           = header,
                    MatchStatus         = "matched",
                    MappingType         = staging.MappingType,
                    CustomerFieldName   = staging.CustomerFieldName,
                    FieldDefinitionId   = staging.FieldDefinitionId,
                    FieldLabel          = fd?.FieldLabel,
                    IsAutoMatched       = false,
                });
                continue;
            }

            if (staging?.Status == "skipped")
            {
                result.Add(new ColumnMatchResultDto { ColumnIndex = i, CsvHeader = header, MatchStatus = "skipped", IsAutoMatched = false });
                continue;
            }

            result.Add(new ColumnMatchResultDto { ColumnIndex = i, CsvHeader = header, MatchStatus = "unmatched", IsAutoMatched = false });
        }

        return result;
    }

    private async Task<Dictionary<Guid, FieldDefinition>> GetFieldDefsForBatch(List<ImportColumnMapping> mappings)
    {
        var ids = mappings.Where(m => m.FieldDefinitionId.HasValue).Select(m => m.FieldDefinitionId!.Value).Distinct();
        var result = new Dictionary<Guid, FieldDefinition>();
        foreach (var id in ids)
        {
            var fd = await fieldRepo.GetByIdAsync(id);
            if (fd is not null) result[id] = fd;
        }
        return result;
    }

    private static (string? FirstName, string? LastName, string? MiddleName, string? Email, string? OriginalId)
        ExtractCustomerFields(string?[] row, List<ImportColumnMapping> mappings)
    {
        string? fn = null, ln = null, mn = null, email = null, origId = null;
        foreach (var m in mappings.Where(m => m.MappingType == "customer_field"))
        {
            var value = m.CsvColumnIndex < row.Length ? row[m.CsvColumnIndex]?.Trim() : null;
            switch (m.CustomerFieldName)
            {
                case "FirstName":   fn      = value; break;
                case "LastName":    ln      = value; break;
                case "MiddleName":  mn      = value; break;
                case "Email":       email   = value?.ToLowerInvariant(); break;
                case "OriginalId":  origId  = value; break;
            }
        }
        return (fn, ln, mn, email, origId);
    }

    private static List<(Guid FieldDefinitionId, string? Value)> ExtractFieldValues(
        string?[] row, List<ImportColumnMapping> mappings)
    {
        var result = new List<(Guid, string?)>();
        foreach (var m in mappings.Where(m => m.MappingType == "field_definition" && m.FieldDefinitionId.HasValue))
        {
            var value = m.CsvColumnIndex < row.Length ? row[m.CsvColumnIndex]?.Trim() : null;
            result.Add((m.FieldDefinitionId!.Value, value));
        }
        return result;
    }

    private static (string Status, string? Message) ValidateRow(
        string?[] row, List<ImportColumnMapping> mappings, Dictionary<Guid, FieldDefinition> fieldDefs)
    {
        foreach (var m in mappings.Where(m => m.IsRequired || m.MappingType == "customer_field"))
        {
            var value = m.CsvColumnIndex < row.Length ? row[m.CsvColumnIndex]?.Trim() : null;
            if (string.IsNullOrEmpty(value))
            {
                if (m.MappingType == "customer_field" && m.CustomerFieldName is "FirstName" or "LastName")
                    return ("error", $"{m.CustomerFieldName} is required.");
                if (m.IsRequired)
                    return ("error", $"'{m.CsvHeader}' is required but empty.");
            }
        }
        return ("ok", null);
    }

    private static string ComputeFingerprint(string[] headers)
    {
        var sorted  = string.Join(",", headers.Select(h => h.Trim().ToLowerInvariant()).OrderBy(h => h));
        var hash    = SHA256.HashData(Encoding.UTF8.GetBytes(sorted));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateCustomerCode(string abbreviation)
        => $"{abbreviation}-{Ulid.NewUlid().ToString()[..10]}";

    private static ImportBatchDto MapBatch(ImportBatch b) => new()
    {
        BatchId             = b.BatchId,
        OrganizationId      = b.OrganizationId,
        FileName            = b.FileName,
        FileType            = b.FileType,
        TotalRows           = b.TotalRows,
        ImportedRows        = b.ImportedRows,
        SkippedRows         = b.SkippedRows,
        ErrorRows           = b.ErrorRows,
        Status              = b.Status,
        DuplicateStrategy   = b.DuplicateStrategy,
        UploadedBy          = b.UploadedBy,
        UploadedAt          = b.UploadedAt,
        CompletedAt         = b.CompletedAt,
        Notes               = b.Notes,
    };
}
