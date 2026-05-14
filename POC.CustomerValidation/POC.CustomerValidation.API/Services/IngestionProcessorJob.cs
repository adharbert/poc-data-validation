using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

/// <summary>
/// Background service that processes IngestionJobs.
///
/// For each Pending job it:
///   1. Parses the file (CSV or XLSX)
///   2. Auto-matches column headers to known customer / address / field-def destinations
///   3. Calculates a mapping confidence score
///   4. Routes to tier: Auto (>=0.92) | Review (>=0.75) | ETL (<0.75)
///   5. Normalises every row into IngestionStagingRows (JSON)
///   6. For Auto-tier jobs: immediately calls CommitJobAsync
///   7. For Review/ETL jobs: sets status to AwaitingReview / AwaitingETL
///
/// AI column mapping (AiMappingService) will slot in at step 2 once the
/// Anthropic API key is configured.
/// </summary>
public class IngestionProcessorJob(
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionProcessorJob> log) : BackgroundService
{
    private static readonly string[] CustomerFields =
        ["FirstName", "LastName", "MiddleName", "MaidenName", "DateOfBirth", "Email", "Phone", "OriginalId"];

    private static readonly string[] AddressFields =
        ["AddressLine1", "AddressLine2", "City", "State", "PostalCode", "Country", "AddressType"];

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        log.LogInformation("IngestionProcessorJob started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IIngestionRepository>();

                var job = await repo.DequeueNextPendingJobAsync();
                if (job is not null)
                {
                    await ProcessJobAsync(job, scope.ServiceProvider, ct);
                }
                else
                {
                    await Task.Delay(5_000, ct);
                }
            }
            catch (OperationCanceledException) { /* shutdown */ }
            catch (Exception ex)
            {
                log.LogError(ex, "Unhandled error in IngestionProcessorJob loop.");
                await Task.Delay(5_000, ct);
            }
        }

        log.LogInformation("IngestionProcessorJob stopped.");
    }

    private async Task ProcessJobAsync(IngestionJob job, IServiceProvider sp, CancellationToken ct)
    {
        var repo        = sp.GetRequiredService<IIngestionRepository>();
        var fieldRepo   = sp.GetRequiredService<IFieldDefinitionRepository>();
        var jobSvc      = sp.GetRequiredService<IIngestionJobService>();

        log.LogInformation("Processing IngestionJob {JobId}: {FileName}", job.Id, job.FileName);

        try
        {
            if (string.IsNullOrEmpty(job.FileStoragePath) || !File.Exists(job.FileStoragePath))
                throw new FileNotFoundException($"File not found: {job.FileStoragePath}");

            // Step 1 — parse file to get headers + all rows
            var (headers, allRows) = ReadFile(job.FileStoragePath, job.FileType);

            // Step 2 — update fingerprint now that we have the actual headers
            job.HeaderFingerprint = ComputeFingerprint(headers);

            // Step 3 — auto-match columns
            var fieldDefs   = (await fieldRepo.GetByOrganizationIdAsync(job.OrganizationId)).ToList();
            var mappings    = AutoMatch(headers, fieldDefs);
            var confidence  = ComputeConfidence(mappings, headers.Length);

            job.MappingJson = JsonSerializer.Serialize(mappings);

            // Step 4 — tier routing
            job.Tier = confidence >= 0.92m ? "Auto" :
                       confidence >= 0.75m ? "Review" : "ETL";

            // Step 5 — normalise rows into staging
            var stagingRows = new List<IngestionStagingRow>();
            int rowNum = 0;

            foreach (var row in allRows)
            {
                ct.ThrowIfCancellationRequested();
                rowNum++;
                var (rowJson, flags) = NormaliseRow(row, mappings, fieldDefs, headers);
                var rowConfidence    = flags.Length == 0 ? confidence : confidence * 0.8m;

                stagingRows.Add(new IngestionStagingRow
                {
                    IngestionJobId  = job.Id,
                    RowNumber       = rowNum,
                    RowJson         = rowJson,
                    ConfidenceScore = rowConfidence,
                    Status          = flags.Length == 0 ? "Pass" : "Flagged",
                    FlagReasons     = flags.Length > 0 ? JsonSerializer.Serialize(flags) : null,
                });
            }

            job.TotalRows   = rowNum;
            job.PassedRows  = stagingRows.Count(r => r.Status == "Pass");
            job.FlaggedRows = stagingRows.Count(r => r.Status == "Flagged");
            job.FailedRows  = 0;

            if (stagingRows.Count > 0)
                await repo.CreateStagingRowsAsync(stagingRows);

            // Step 6 — route
            if (job.Tier == "Auto" && job.FlaggedRows == 0)
            {
                job.Status = "Processing"; // stays Processing while CommitJobAsync runs
                await repo.UpdateJobAsync(job);
                await jobSvc.CommitJobAsync(job.Id, "System");
            }
            else
            {
                job.Status = job.Tier == "ETL" ? "AwaitingETL" : "AwaitingReview";
                await repo.UpdateJobAsync(job);
            }

            log.LogInformation(
                "IngestionJob {JobId} staged: {Total} rows, tier={Tier}, confidence={Confidence:P0}",
                job.Id, rowNum, job.Tier, confidence);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "IngestionJob {JobId} failed during processing.", job.Id);
            job.Status       = "Failed";
            job.ErrorMessage = ex.Message;
            await repo.UpdateJobAsync(job);
        }
    }

    // ------------------------------------------------------------------
    // Column auto-matching — same heuristics as ImportService
    // ------------------------------------------------------------------

    private static List<ColumnMapping> AutoMatch(string[] headers, List<FieldDefinition> fieldDefs)
    {
        var result = new List<ColumnMapping>();

        for (int i = 0; i < headers.Length; i++)
        {
            var h = headers[i].Trim();

            var custField = CustomerFields.FirstOrDefault(f => f.Equals(h, StringComparison.OrdinalIgnoreCase));
            if (custField is not null)
            {
                result.Add(new ColumnMapping(i, h, "customer", custField, null, "direct", 1.0m));
                continue;
            }

            var addrField = AddressFields.FirstOrDefault(f => f.Equals(h, StringComparison.OrdinalIgnoreCase));
            if (addrField is not null)
            {
                result.Add(new ColumnMapping(i, h, "customer_address", addrField, null, "direct", 0.95m));
                continue;
            }

            var byKey = fieldDefs.FirstOrDefault(f => f.FieldKey.Equals(h, StringComparison.OrdinalIgnoreCase));
            if (byKey is not null)
            {
                result.Add(new ColumnMapping(i, h, "field_value", null, byKey.FieldDefinitionId, "direct", 0.90m));
                continue;
            }

            var byLabel = fieldDefs.FirstOrDefault(f => f.FieldLabel.Equals(h, StringComparison.OrdinalIgnoreCase));
            if (byLabel is not null)
            {
                result.Add(new ColumnMapping(i, h, "field_value", null, byLabel.FieldDefinitionId, "direct", 0.85m));
                continue;
            }

            result.Add(new ColumnMapping(i, h, "skip", null, null, "direct", 0.0m));
        }

        return result;
    }

    private static decimal ComputeConfidence(List<ColumnMapping> mappings, int totalHeaders)
    {
        if (totalHeaders == 0) return 0m;
        var matched = mappings.Count(m => m.Destination != "skip");
        if (matched == 0) return 0m;
        var avgConfidence = mappings.Where(m => m.Destination != "skip").Average(m => m.Confidence);
        var coverageRatio = (decimal)matched / totalHeaders;
        return Math.Round(avgConfidence * coverageRatio, 4);
    }

    // ------------------------------------------------------------------
    // Row normalisation — returns (rowJson, flagReasons[])
    // ------------------------------------------------------------------

    private static (string RowJson, string[] Flags) NormaliseRow(
        string?[] row, List<ColumnMapping> mappings,
        List<FieldDefinition> fieldDefs, string[] headers)
    {
        string? fn = null, ln = null, mn = null, maiden = null, email = null, phone = null, origId = null;
        DateOnly? dob = null;

        string? addr1 = null, addr2 = null, city = null, state = null, zip = null, country = null, addrType = null;

        var fieldValues = new List<StagingFieldValue>();
        var flags       = new List<string>();

        foreach (var m in mappings.Where(m => m.Destination != "skip"))
        {
            var raw = m.ColumnIndex < row.Length ? row[m.ColumnIndex]?.Trim() : null;

            switch (m.Destination)
            {
                case "customer":
                    switch (m.DestinationField)
                    {
                        case "FirstName":   fn      = raw;  break;
                        case "LastName":    ln      = raw;  break;
                        case "MiddleName":  mn      = raw;  break;
                        case "MaidenName":  maiden  = raw;  break;
                        case "Email":       email   = raw?.ToLowerInvariant();  break;
                        case "Phone":       phone   = string.IsNullOrWhiteSpace(raw) ? null
                                                        : Regex.Replace(raw, @"\D", "");  break;
                        case "OriginalId":  origId  = raw;  break;
                        case "DateOfBirth":
                            if (!string.IsNullOrWhiteSpace(raw) && DateOnly.TryParse(raw, out var d))
                                dob = d;
                            break;
                    }
                    break;

                case "customer_address":
                    if (string.IsNullOrWhiteSpace(raw)) break;
                    switch (m.DestinationField)
                    {
                        case "AddressLine1": addr1    = raw;  break;
                        case "AddressLine2": addr2    = raw;  break;
                        case "City":         city     = raw;  break;
                        case "State":        state    = raw;  break;
                        case "PostalCode":   zip      = raw;  break;
                        case "Country":      country  = raw;  break;
                        case "AddressType":  addrType = raw;  break;
                    }
                    break;

                case "field_value" when m.FieldDefinitionId.HasValue:
                    fieldValues.Add(new StagingFieldValue(m.FieldDefinitionId.Value, raw));
                    break;
            }
        }

        // Validation flags
        if (string.IsNullOrWhiteSpace(fn))  flags.Add("FirstName is missing.");
        if (string.IsNullOrWhiteSpace(ln))  flags.Add("LastName is missing.");
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            flags.Add("No contact information (Email or Phone) found.");

        var customerData = new
        {
            FirstName   = fn,
            LastName    = ln,
            MiddleName  = mn,
            MaidenName  = maiden,
            DateOfBirth = dob,
            Email       = email,
            Phone       = phone,
            OriginalId  = origId,
        };

        object? addressData = (addr1 is not null || city is not null) ? new
        {
            AddressLine1 = addr1,
            AddressLine2 = addr2,
            City         = city,
            State        = state,
            PostalCode   = zip,
            Country      = country ?? "US",
            AddressType  = addrType ?? "primary",
        } : null;

        var rowData = new
        {
            Customer    = customerData,
            Address     = addressData,
            FieldValues = fieldValues.Select(f => new { f.FieldDefinitionId, f.Value }),
        };

        return (JsonSerializer.Serialize(rowData), flags.ToArray());
    }

    // ------------------------------------------------------------------
    // File parsing (reuses same logic as ImportService)
    // ------------------------------------------------------------------

    private static (string[] Headers, IEnumerable<string?[]> Rows) ReadFile(string path, string fileType)
    {
        if (fileType == "csv")
        {
            using var reader    = new StreamReader(path);
            using var csv       = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord!;
            var rows    = new List<string?[]>();
            while (csv.Read())
            {
                var record = new string?[headers.Length];
                for (int i = 0; i < record.Length; i++) record[i] = csv.GetField(i);
                rows.Add(record);
            }
            return (headers, rows);
        }
        else
        {
            using var wb        = new XLWorkbook(path);
            var ws              = wb.Worksheets.First();
            var headerRow       = ws.FirstRowUsed();
            if (headerRow is null) return ([], []);
            var headers         = headerRow.CellsUsed().Select(c => c.GetString()).ToArray();
            var colCount        = headers.Length;
            var rows            = ws.RowsUsed().Skip(1)
                .Select(r => Enumerable.Range(1, colCount)
                    .Select(i => (string?)r.Cell(i).GetString())
                    .ToArray())
                .ToList();
            return (headers, rows);
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string ComputeFingerprint(string[] headers)
    {
        var sorted = string.Join(",", headers.Select(h => h.Trim().ToLowerInvariant()).OrderBy(h => h));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sorted))).ToLowerInvariant();
    }

    private record ColumnMapping(
        int         ColumnIndex,
        string      Header,
        string      Destination,
        string?     DestinationField,
        Guid?       FieldDefinitionId,
        string      TransformType,
        decimal     Confidence);

    private record StagingFieldValue(Guid FieldDefinitionId, string? Value);
}
