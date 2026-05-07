using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class IngestionRepository(IDbConnectionFactory db) : IIngestionRepository
{
    // ------------------------------------------------------------------
    // Jobs
    // ------------------------------------------------------------------

    public async Task<IngestionJob> CreateJobAsync(IngestionJob job)
    {
        job.Id         = Guid.NewGuid();
        job.UploadedAt = DateTime.UtcNow;

        const string sql = """
            INSERT INTO IngestionJobs (
                Id, OrganizationId, FileName, FileType, FileSizeBytes, FileHash,
                FileStoragePath, HeaderFingerprint, MappingJson, UploadedBy,
                UploadedAt, Status, TotalRows
            ) VALUES (
                @Id, @OrganizationId, @FileName, @FileType, @FileSizeBytes, @FileHash,
                @FileStoragePath, @HeaderFingerprint, @MappingJson, @UploadedBy,
                @UploadedAt, @Status, @TotalRows
            )
            """;

        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(sql, job);
        return job;
    }

    public async Task<IngestionJob?> GetJobByIdAsync(Guid jobId)
    {
        const string sql = """
            SELECT  Id, OrganizationId, FileName, FileType, FileSizeBytes, FileHash,
                    FileStoragePath, HeaderFingerprint, MappingJson, UploadedBy,
                    UploadedAt, Status, Tier, TotalRows, PassedRows, FlaggedRows,
                    FailedRows, ErrorMessage, CompletedAt
            FROM    IngestionJobs
            WHERE   Id = @JobId
            """;

        using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<IngestionJob>(sql, new { JobId = jobId });
    }

    public async Task<(IEnumerable<IngestionJob> Items, int TotalCount)> GetJobsByOrganizationAsync(
        Guid organizationId, int page, int pageSize)
    {
        const string countSql = "SELECT COUNT(1) FROM IngestionJobs WHERE OrganizationId = @OrgId";
        const string dataSql = """
            SELECT  Id, OrganizationId, FileName, FileType, FileSizeBytes, FileHash,
                    FileStoragePath, HeaderFingerprint, UploadedBy,
                    UploadedAt, Status, Tier, TotalRows, PassedRows, FlaggedRows,
                    FailedRows, ErrorMessage, CompletedAt
            FROM    IngestionJobs
            WHERE   OrganizationId = @OrgId
            ORDER BY UploadedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new { OrgId = organizationId, Offset = (page - 1) * pageSize, PageSize = pageSize };
        using var conn = db.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<IngestionJob>(dataSql, p);
        return (items, total);
    }

    public async Task<IngestionJob?> DequeueNextPendingJobAsync()
    {
        // Atomic claim: only one caller gets a given job even under concurrency.
        const string sql = """
            UPDATE TOP(1) IngestionJobs
            SET    Status = 'Processing'
            OUTPUT INSERTED.Id, INSERTED.OrganizationId, INSERTED.FileName,
                   INSERTED.FileType, INSERTED.FileSizeBytes, INSERTED.FileHash,
                   INSERTED.FileStoragePath, INSERTED.HeaderFingerprint,
                   INSERTED.MappingJson, INSERTED.UploadedBy, INSERTED.UploadedAt,
                   INSERTED.Status, INSERTED.Tier, INSERTED.TotalRows,
                   INSERTED.PassedRows, INSERTED.FlaggedRows, INSERTED.FailedRows,
                   INSERTED.ErrorMessage, INSERTED.CompletedAt
            WHERE  Status = 'Pending'
            """;

        using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<IngestionJob>(sql);
    }

    public async Task UpdateJobAsync(IngestionJob job)
    {
        const string sql = """
            UPDATE IngestionJobs
            SET    Status       = @Status,
                   Tier         = @Tier,
                   MappingJson  = @MappingJson,
                   TotalRows    = @TotalRows,
                   PassedRows   = @PassedRows,
                   FlaggedRows  = @FlaggedRows,
                   FailedRows   = @FailedRows,
                   ErrorMessage = @ErrorMessage,
                   CompletedAt  = @CompletedAt
            WHERE  Id = @Id
            """;

        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(sql, job);
    }

    // ------------------------------------------------------------------
    // Staging rows
    // ------------------------------------------------------------------

    public async Task CreateStagingRowsAsync(IEnumerable<IngestionStagingRow> rows)
    {
        const string sql = """
            INSERT INTO IngestionStagingRows (
                Id, IngestionJobId, RowNumber, RowJson, ConfidenceScore, Status, FlagReasons
            ) VALUES (
                @Id, @IngestionJobId, @RowNumber, @RowJson, @ConfidenceScore, @Status, @FlagReasons
            )
            """;

        var rowList = rows.Select(r =>
        {
            r.Id = Guid.NewGuid();
            return r;
        }).ToList();

        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(sql, rowList);
    }

    public async Task<(IEnumerable<IngestionStagingRow> Items, int TotalCount)> GetStagingRowsAsync(
        Guid jobId, string? statusFilter, int page, int pageSize)
    {
        var where   = statusFilter is not null ? "AND Status = @StatusFilter" : "";
        var countSql = $"SELECT COUNT(1) FROM IngestionStagingRows WHERE IngestionJobId = @JobId {where}";
        var dataSql  = $"""
            SELECT  Id, IngestionJobId, RowNumber, RowJson, ConfidenceScore,
                    Status, FlagReasons, ReviewedBy, ReviewedAt
            FROM    IngestionStagingRows
            WHERE   IngestionJobId = @JobId {where}
            ORDER BY RowNumber
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new { JobId = jobId, StatusFilter = statusFilter, Offset = (page - 1) * pageSize, PageSize = pageSize };
        using var conn = db.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<IngestionStagingRow>(dataSql, p);
        return (items, total);
    }

    public async Task<IEnumerable<IngestionStagingRow>> GetCommittableStagingRowsAsync(Guid jobId)
    {
        const string sql = """
            SELECT  Id, IngestionJobId, RowNumber, RowJson, ConfidenceScore,
                    Status, FlagReasons, ReviewedBy, ReviewedAt
            FROM    IngestionStagingRows
            WHERE   IngestionJobId = @JobId
              AND   Status IN ('Pending','Pass')
            ORDER BY RowNumber
            """;

        using var conn = db.CreateConnection();
        return await conn.QueryAsync<IngestionStagingRow>(sql, new { JobId = jobId });
    }

    public async Task UpdateStagingRowAsync(IngestionStagingRow row)
    {
        const string sql = """
            UPDATE IngestionStagingRows
            SET    Status      = @Status,
                   FlagReasons = @FlagReasons,
                   ReviewedBy  = @ReviewedBy,
                   ReviewedAt  = @ReviewedAt
            WHERE  Id = @Id
            """;

        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(sql, row);
    }

    public async Task MarkStagingRowsCommittedAsync(IEnumerable<Guid> rowIds)
    {
        const string sql = """
            UPDATE IngestionStagingRows
            SET    Status = 'Committed'
            WHERE  Id IN @Ids
            """;

        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(sql, new { Ids = rowIds });
    }
}
