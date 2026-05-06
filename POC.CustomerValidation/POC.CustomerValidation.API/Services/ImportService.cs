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
    ICustomerPhoneRepository phoneRepo,
    ICustomerEmailRepository emailRepo,
    ICustomerAddressRepository customerAddressRepo,
    IFieldValueRepository fieldValueRepo,
    IConfiguration config,
    ILogger<ImportService> log) : IImportService
{
    private static readonly string[] CustomerFields =
        ["FirstName", "LastName", "MiddleName", "MaidenName", "DateOfBirth", "Email", "Phone", "OriginalId"];

    private static readonly string[] AddressFields =
        ["AddressLine1", "AddressLine2", "City", "State", "PostalCode", "Country", "AddressType", "Latitude", "Longitude"];

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

        // Schema drift detection — compare current headers against the org's saved template
        var (missingMapped, newCols) = DetectSchemaDrift(headers, savedMappings);
        var schemaDrift = missingMapped.Length > 0;

        if (schemaDrift)
            log.LogWarning(
                "Schema drift detected for org {OrgId}: missing mapped columns [{Missing}]",
                organisationId, string.Join(", ", missingMapped));

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
            BatchId              = batch.BatchId,
            Headers              = headers,
            RowCount             = rowCount,
            HasSavedMappings     = hasSaved,
            ColumnMatches        = matches,
            SchemaDrift          = schemaDrift,
            MissingMappedColumns = missingMapped,
            NewColumns           = newCols,
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
                DestinationTable    = s.DestinationTable,
                DestinationField    = s.DestinationField,
                FieldDefinitionId   = s.FieldDefinitionId,
                TransformType       = s.TransformType,
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
            DestinationTable    = m.DestinationTable,
            DestinationField    = m.DestinationField,
            FieldDefinitionId   = m.FieldDefinitionId,
            TransformType       = m.TransformType,
            IsAutoMatched       = false,
            SavedForReuse       = m.SaveForReuse,
            DisplayOrder        = i,
            Outputs             = m.Outputs.Select(o => new ImportColumnMappingOutput
            {
                OutputToken         = o.OutputToken,
                DestinationTable    = o.DestinationTable,
                DestinationField    = o.DestinationField,
                FieldDefinitionId   = o.FieldDefinitionId,
                SortOrder           = o.SortOrder,
            }).ToList(),
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
        var outputs     = (await importRepo.GetMappingOutputsByBatchIdAsync(batchId)).ToList();
        // Attach outputs to their parent mappings for use in ExtractCustomerFields
        var outputsByMapping = outputs.GroupBy(o => o.MappingId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var m in mappings)
            if (outputsByMapping.TryGetValue(m.MappingId, out var mo))
                m.Outputs = mo;
        var fieldDefs   = await GetFieldDefsForBatch(mappings);
        var allRows     = ReadFileRows(batch.FileStoragePath!, batch.FileType!);

        var abbreviation    = (org!.Abbreviation ?? org.OrganizationCode[..Math.Min(4, org.OrganizationCode.Length)]).ToUpperInvariant().Trim();
        int imported = 0, skipped = 0, errors = 0;

        foreach (var (row, rowNum) in allRows.Select((r, i) => (r, i + 1)))
        {
            try
            {
                var customerData    = ExtractCustomerFields(row, mappings);
                var addressData     = ExtractAddressFields(row, mappings);
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
                        MaidenName      = customerData.MaidenName,
                        DateOfBirth     = customerData.DateOfBirth,
                        OriginalId      = customerData.OriginalId,
                        Email           = customerData.Email,
                        Phone           = customerData.Phone,
                        CustomerCode    = code,
                        IsActive        = true,
                    };
                    existing = await customerRepo.CreateAsync(customer);

                    if (!string.IsNullOrWhiteSpace(customerData.Phone))
                        await phoneRepo.CreateAsync(new CustomerPhone
                        {
                            CustomerId  = existing.CustomerId,
                            PhoneNumber = customerData.Phone,
                            PhoneType   = "mobile",
                            IsPrimary   = true,
                        });

                    if (!string.IsNullOrWhiteSpace(customerData.Email))
                        await emailRepo.CreateAsync(new CustomerEmail
                        {
                            CustomerId   = existing.CustomerId,
                            EmailAddress = customerData.Email,
                            EmailType    = "personal",
                            IsPrimary    = true,
                        });

                    if (addressData is not null)
                    {
                        addressData.CustomerId = existing.CustomerId;
                        await customerAddressRepo.CreateAsync(addressData);
                    }
                }
                else if (batch.DuplicateStrategy == "update")
                {
                    existing.FirstName   = customerData.FirstName  ?? existing.FirstName;
                    existing.LastName    = customerData.LastName   ?? existing.LastName;
                    existing.MiddleName  = customerData.MiddleName ?? existing.MiddleName;
                    existing.MaidenName  = customerData.MaidenName ?? existing.MaidenName;
                    existing.DateOfBirth = customerData.DateOfBirth ?? existing.DateOfBirth;
                    existing.OriginalId  = customerData.OriginalId ?? existing.OriginalId;
                    existing.Email       = customerData.Email  ?? existing.Email;
                    existing.Phone       = customerData.Phone  ?? existing.Phone;
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
            DestinationTable    = m.DestinationTable,
            DestinationField    = m.DestinationField,
            FieldDefinitionId   = m.FieldDefinitionId,
            TransformType       = m.TransformType,
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
                    DestinationTable    = saved.DestinationTable,
                    DestinationField    = saved.DestinationField,
                    FieldDefinitionId   = saved.FieldDefinitionId,
                    TransformType       = saved.TransformType,
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
                    DestinationTable    = "customer",
                    DestinationField    = custField,
                    TransformType       = "direct",
                    IsAutoMatched       = true,
                });
                continue;
            }

            // Address field match (case-insensitive)
            var addrField = AddressFields.FirstOrDefault(f => f.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (addrField is not null)
            {
                result.Add(new ColumnMatchResultDto
                {
                    ColumnIndex         = i,
                    CsvHeader           = header,
                    MatchStatus         = "matched",
                    DestinationTable    = "customer_address",
                    DestinationField    = addrField,
                    TransformType       = "direct",
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
                    DestinationTable    = "field_value",
                    FieldDefinitionId   = byKey.FieldDefinitionId,
                    TransformType       = "direct",
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
                    DestinationTable    = "field_value",
                    FieldDefinitionId   = byLabel.FieldDefinitionId,
                    TransformType       = "direct",
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
                // Staging table still uses legacy MappingType/CustomerFieldName — translate on read
                var stagingDestTable = staging.MappingType switch
                {
                    "customer_field"   => "customer",
                    "field_definition" => "field_value",
                    _                  => "skip",
                };
                var fd = staging.FieldDefinitionId.HasValue
                    ? fieldDefs.FirstOrDefault(f => f.FieldDefinitionId == staging.FieldDefinitionId.Value)
                    : null;
                result.Add(new ColumnMatchResultDto
                {
                    ColumnIndex         = i,
                    CsvHeader           = header,
                    MatchStatus         = "matched",
                    DestinationTable    = stagingDestTable,
                    DestinationField    = staging.CustomerFieldName,
                    FieldDefinitionId   = staging.FieldDefinitionId,
                    TransformType       = "direct",
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

    private static (string? FirstName, string? LastName, string? MiddleName, string? MaidenName,
                    DateOnly? DateOfBirth, string? Email, string? Phone, string? OriginalId)
        ExtractCustomerFields(string?[] row, List<ImportColumnMapping> mappings)
    {
        string? fn = null, ln = null, mn = null, maiden = null, email = null, phone = null, origId = null;
        DateOnly? dob = null;

        foreach (var m in mappings.Where(m => m.DestinationTable == "customer"))
        {
            var rawValue = m.CsvColumnIndex < row.Length ? row[m.CsvColumnIndex]?.Trim() : null;

            if (m.TransformType == "split_full_name")
            {
                var (pFirst, pMiddle, pLast, _, _) = FullNameParser.Parse(rawValue);
                foreach (var o in m.Outputs)
                {
                    if (o.DestinationTable != "customer") continue;
                    var parsed = o.OutputToken switch
                    {
                        "FirstName"  => pFirst,
                        "MiddleName" => pMiddle,
                        "LastName"   => pLast,
                        _            => null,
                    };
                    ApplyCustomerField(o.DestinationField, parsed, ref fn, ref ln, ref mn, ref maiden, ref email, ref phone, ref origId, ref dob);
                }
            }
            else
            {
                ApplyCustomerField(m.DestinationField, rawValue, ref fn, ref ln, ref mn, ref maiden, ref email, ref phone, ref origId, ref dob);
            }
        }
        return (fn, ln, mn, maiden, dob, email, phone, origId);
    }

    private static void ApplyCustomerField(
        string? fieldName, string? value,
        ref string? fn, ref string? ln, ref string? mn, ref string? maiden,
        ref string? email, ref string? phone, ref string? origId, ref DateOnly? dob)
    {
        switch (fieldName)
        {
            case "FirstName":   fn      = value; break;
            case "LastName":    ln      = value; break;
            case "MiddleName":  mn      = value; break;
            case "MaidenName":  maiden  = value; break;
            case "Email":       email   = value?.ToLowerInvariant(); break;
            case "Phone":       phone   = string.IsNullOrWhiteSpace(value) ? null
                                            : Regex.Replace(value, @"\D", ""); break;
            case "OriginalId":  origId  = value; break;
            case "DateOfBirth":
                if (!string.IsNullOrWhiteSpace(value) && DateOnly.TryParse(value, out var parsed))
                    dob = parsed;
                break;
        }
    }

    private static CustomerAddress? ExtractAddressFields(string?[] row, List<ImportColumnMapping> mappings)
    {
        var addrMappings = mappings.Where(m => m.DestinationTable == "customer_address").ToList();
        if (addrMappings.Count == 0) return null;

        var addr = new CustomerAddress { AddressType = "primary" };
        bool hasData = false;

        foreach (var m in addrMappings)
        {
            var value = m.CsvColumnIndex < row.Length ? row[m.CsvColumnIndex]?.Trim() : null;
            if (string.IsNullOrWhiteSpace(value)) continue;
            hasData = true;

            if (m.TransformType == "split_full_address")
            {
                var parsed = AddressParser.Parse(value);
                foreach (var o in m.Outputs)
                {
                    if (o.DestinationTable != "customer_address") continue;
                    var tokenValue = o.OutputToken switch
                    {
                        "AddressLine1" => parsed.AddressLine1,
                        "AddressLine2" => parsed.AddressLine2,
                        "City"         => parsed.City,
                        "State"        => parsed.State,
                        "PostalCode"   => parsed.PostalCode,
                        "Country"      => parsed.Country,
                        _              => null,
                    };
                    ApplyAddressField(o.DestinationField, tokenValue, addr);
                }
            }
            else
            {
                ApplyAddressField(m.DestinationField, value, addr);
            }
        }

        // Require at minimum AddressLine1 and City to create a record
        return hasData && !string.IsNullOrWhiteSpace(addr.AddressLine1) && !string.IsNullOrWhiteSpace(addr.City)
            ? addr
            : null;
    }

    private static void ApplyAddressField(string? field, string? value, CustomerAddress addr)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        switch (field)
        {
            case "AddressLine1": addr.AddressLine1 = value; break;
            case "AddressLine2": addr.AddressLine2 = value; break;
            case "City":         addr.City         = value; break;
            case "State":        addr.State        = value.Length > 2
                                     ? StateLookup.ToCode(value) ?? value[..2].ToUpper()
                                     : value.ToUpper(); break;
            case "PostalCode":   addr.PostalCode   = value; break;
            case "Country":      addr.Country      = value; break;
            case "AddressType":  addr.AddressType  = value; break;
            case "Latitude":
                if (double.TryParse(value, out var lat)) addr.Latitude = lat;
                break;
            case "Longitude":
                if (double.TryParse(value, out var lng)) addr.Longitude = lng;
                break;
        }
    }

    private static List<(Guid FieldDefinitionId, string? Value)> ExtractFieldValues(
        string?[] row, List<ImportColumnMapping> mappings)
    {
        var result = new List<(Guid, string?)>();
        foreach (var m in mappings.Where(m => m.DestinationTable == "field_value" && m.FieldDefinitionId.HasValue))
        {
            var value = m.CsvColumnIndex < row.Length ? row[m.CsvColumnIndex]?.Trim() : null;
            result.Add((m.FieldDefinitionId!.Value, value));
        }
        return result;
    }

    private static (string Status, string? Message) ValidateRow(
        string?[] row, List<ImportColumnMapping> mappings, Dictionary<Guid, FieldDefinition> fieldDefs)
    {
        foreach (var m in mappings.Where(m => m.IsRequired || m.DestinationTable == "customer"))
        {
            var value = m.CsvColumnIndex < row.Length ? row[m.CsvColumnIndex]?.Trim() : null;
            if (string.IsNullOrEmpty(value))
            {
                if (m.DestinationTable == "customer" && m.DestinationField is "FirstName" or "LastName"
                    && m.TransformType == "direct")
                    return ("error", $"{m.DestinationField} is required.");
                if (m.IsRequired)
                    return ("error", $"'{m.CsvHeader}' is required but empty.");
            }
        }

        // Contactability: at least one of Email or Phone required when either is mapped
        var contactMappings = mappings
            .Where(m => m.DestinationTable == "customer" && m.DestinationField is "Email" or "Phone")
            .ToList();

        if (contactMappings.Count > 0)
        {
            var hasContact = contactMappings.Any(m =>
            {
                var v = m.CsvColumnIndex < row.Length ? row[m.CsvColumnIndex]?.Trim() : null;
                return !string.IsNullOrEmpty(v);
            });
            if (!hasContact)
                return ("error", "At least one contact method (Phone or Email) is required.");
        }

        return ("ok", null);
    }

    private static (string[] Missing, string[] New) DetectSchemaDrift(
        string[] currentHeaders, List<SavedColumnMapping> savedMappings)
    {
        if (savedMappings.Count == 0)
            return ([], []);

        var savedHeaders    = savedMappings.Select(s => s.CsvHeader.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentSet      = currentHeaders.Select(h => h.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Columns that were previously mapped (not skipped) but are missing now
        var missing = savedMappings
            .Where(s => s.DestinationTable != "skip" && !currentSet.Contains(s.CsvHeader.Trim()))
            .Select(s => s.CsvHeader)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Columns in the current file that weren't in the saved mapping
        var newCols = currentHeaders
            .Where(h => !savedHeaders.Contains(h.Trim()))
            .ToArray();

        return (missing, newCols);
    }

    private static string ComputeFingerprint(string[] headers)
    {
        var sorted  = string.Join(",", headers.Select(h => h.Trim().ToLowerInvariant()).OrderBy(h => h));
        var hash    = SHA256.HashData(Encoding.UTF8.GetBytes(sorted));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateCustomerCode(string abbreviation)
        => $"{abbreviation}-{Ulid.NewUlid().ToString()[..10]}";

    // ------------------------------------------------------------------
    // Name / state helpers
    // ------------------------------------------------------------------

    private static class FullNameParser
    {
        private static readonly Regex CredentialRx = new(
            @"^(?:M\.D\.|D\.O\.|Ph\.D\.|M\.P\.H\.|M\.B\.A\.|D\.D\.S\.|D\.V\.M\.|R\.N\.|N\.P\.|Esq\.?)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SuffixRx = new(
            @"^(?:Jr\.|Sr\.|III|II|IV)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DeceasedRx = new(
            @"\s*\(deceased\)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static (string? First, string? Middle, string? Last, string? Suffix, string? Credentials)
            Parse(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (null, null, null, null, null);

            // Split on " , " to separate name from trailing credential/suffix tokens
            var parts = fullName.Split(" , ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            string? suffix = null;
            var credParts  = new List<string>();
            while (parts.Count > 1)
            {
                var tail = parts[^1];
                if (CredentialRx.IsMatch(tail))      { credParts.Insert(0, tail); parts.RemoveAt(parts.Count - 1); }
                else if (SuffixRx.IsMatch(tail))     { suffix = tail;              parts.RemoveAt(parts.Count - 1); }
                else break;
            }

            var namePart = DeceasedRx.Replace(string.Join(" ", parts), " ").Trim();
            var words    = namePart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var creds    = credParts.Count > 0 ? string.Join(", ", credParts) : null;

            return words.Length switch
            {
                0 => (null,     null,                              null,       suffix, creds),
                1 => (words[0], null,                              null,       suffix, creds),
                2 => (words[0], null,                              words[1],   suffix, creds),
                _ => (words[0], string.Join(" ", words[1..^1]),    words[^1],  suffix, creds),
            };
        }
    }

    private static class AddressParser
    {
        // Matches "IL 62701", "IL62701", "IL 62701-1234"
        private static readonly Regex StateZipRx = new(
            @"^([A-Za-z]{2})\s*(\d{5}(?:-\d{4})?)$",
            RegexOptions.Compiled);

        private static readonly Regex ZipRx = new(
            @"^\d{5}(?:-\d{4})?$",
            RegexOptions.Compiled);

        private static readonly HashSet<string> StateCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA","HI","ID","IL","IN","IA",
            "KS","KY","LA","ME","MD","MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ",
            "NM","NY","NC","ND","OH","OK","OR","PA","RI","SC","SD","TN","TX","UT","VT",
            "VA","WA","WV","WI","WY","DC","PR","VI","GU","AS","MP"
        };

        // Only unambiguous country tokens — "CA" (Canada) is excluded to avoid conflict with
        // the California state abbreviation; "AU" excluded for same reason (Australia vs. no-state-code).
        private static readonly HashSet<string> CountryTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "USA","US","UNITED STATES","UNITED STATES OF AMERICA",
            "CANADA","UK","UNITED KINGDOM","AUSTRALIA","MEXICO","MEX"
        };

        public record ParsedAddress(
            string? AddressLine1, string? AddressLine2,
            string? City, string? State, string? PostalCode, string? Country);

        public static ParsedAddress Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new ParsedAddress(null, null, null, null, null, null);

            var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            string? country = null;
            if (parts.Length > 0 && CountryTokens.Contains(parts[^1]))
            {
                country = parts[^1];
                parts   = parts[..^1];
            }

            return parts.Length switch
            {
                0 => new ParsedAddress(null, null, null, null, null, country),
                1 => ParseOnePart(parts[0], country),
                2 => ParseTwoParts(parts[0], parts[1], country),
                3 => ParseThreeParts(parts[0], parts[1], parts[2], country),
                _ => ParseManyParts(parts, country),
            };
        }

        // "123 Main St Springfield IL 62701"
        private static ParsedAddress ParseOnePart(string part, string? country)
        {
            var words = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                if (ZipRx.IsMatch(words[^1]) && StateCodes.Contains(words[^2]))
                    return new ParsedAddress(string.Join(' ', words[..^2]), null, null,
                        words[^2].ToUpper(), words[^1], country);

                var m = StateZipRx.Match(words[^1]);
                if (m.Success)
                    return new ParsedAddress(string.Join(' ', words[..^1]), null, null,
                        m.Groups[1].Value.ToUpper(), m.Groups[2].Value, country);
            }
            return new ParsedAddress(part, null, null, null, null, country);
        }

        // "123 Main St, Springfield IL 62701"
        private static ParsedAddress ParseTwoParts(string addr, string cityStateZip, string? country)
        {
            var words = cityStateZip.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 3 && ZipRx.IsMatch(words[^1]) && StateCodes.Contains(words[^2]))
                return new ParsedAddress(addr, null,
                    string.Join(' ', words[..^2]), words[^2].ToUpper(), words[^1], country);

            if (words.Length == 2 && StateCodes.Contains(words[0]) && ZipRx.IsMatch(words[1]))
                return new ParsedAddress(addr, null, null, words[0].ToUpper(), words[1], country);

            return new ParsedAddress(addr, null, cityStateZip, null, null, country);
        }

        // "123 Main St, Springfield, IL 62701"  — most common US format
        private static ParsedAddress ParseThreeParts(string addr, string city, string stateZip, string? country)
        {
            var (state, zip) = SplitStateZip(stateZip);
            return new ParsedAddress(addr, null, city, state, zip, country);
        }

        // "123 Main St, Suite 200, Springfield, IL 62701"
        private static ParsedAddress ParseManyParts(string[] parts, string? country)
        {
            var (state, zip) = SplitStateZip(parts[^1]);
            if (state != null || zip != null)
            {
                return new ParsedAddress(
                    parts[0],
                    parts.Length > 3 ? string.Join(", ", parts[1..^2]) : null,
                    parts[^2], state, zip, country);
            }
            // Fallback — assign positionally
            return new ParsedAddress(parts[0],
                parts.Length > 3 ? parts[1] : null,
                parts.Length >= 3 ? parts[^2] : null,
                parts[^1], null, country);
        }

        private static (string? State, string? Zip) SplitStateZip(string s)
        {
            var t = s.Trim();
            var m = StateZipRx.Match(t);
            if (m.Success) return (m.Groups[1].Value.ToUpper(), m.Groups[2].Value);
            if (ZipRx.IsMatch(t)) return (null, t);
            if (t.Length == 2 && StateCodes.Contains(t)) return (t.ToUpper(), null);
            var words = t.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 2 && StateCodes.Contains(words[0]) && ZipRx.IsMatch(words[1]))
                return (words[0].ToUpper(), words[1]);
            return (null, null);
        }
    }

    private static class StateLookup
    {
        private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Alabama"]="AL",["Alaska"]="AK",["Arizona"]="AZ",["Arkansas"]="AR",["California"]="CA",
            ["Colorado"]="CO",["Connecticut"]="CT",["Delaware"]="DE",["Florida"]="FL",["Georgia"]="GA",
            ["Hawaii"]="HI",["Idaho"]="ID",["Illinois"]="IL",["Indiana"]="IN",["Iowa"]="IA",
            ["Kansas"]="KS",["Kentucky"]="KY",["Louisiana"]="LA",["Maine"]="ME",["Maryland"]="MD",
            ["Massachusetts"]="MA",["Michigan"]="MI",["Minnesota"]="MN",["Mississippi"]="MS",
            ["Missouri"]="MO",["Montana"]="MT",["Nebraska"]="NE",["Nevada"]="NV",["New Hampshire"]="NH",
            ["New Jersey"]="NJ",["New Mexico"]="NM",["New York"]="NY",["North Carolina"]="NC",
            ["North Dakota"]="ND",["Ohio"]="OH",["Oklahoma"]="OK",["Oregon"]="OR",["Pennsylvania"]="PA",
            ["Rhode Island"]="RI",["South Carolina"]="SC",["South Dakota"]="SD",["Tennessee"]="TN",
            ["Texas"]="TX",["Utah"]="UT",["Vermont"]="VT",["Virginia"]="VA",["Washington"]="WA",
            ["West Virginia"]="WV",["Wisconsin"]="WI",["Wyoming"]="WY",["District of Columbia"]="DC",
        };

        public static string? ToCode(string fullName) =>
            _map.TryGetValue(fullName.Trim(), out var code) ? code : null;
    }

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
