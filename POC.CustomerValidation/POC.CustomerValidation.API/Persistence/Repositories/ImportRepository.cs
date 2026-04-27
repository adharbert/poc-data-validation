using System.Text.Json;
using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class ImportRepository(IDbConnectionFactory db) : IImportRepository
{
    private readonly IDbConnectionFactory _db = db;

    public async Task<(IEnumerable<ImportBatch> Items, int TotalCount)> GetBatchesByOrganisationAsync(
        Guid organisationId, int page = 1, int pageSize = 20)
    {
        const string countSql = "SELECT COUNT(1) FROM ImportBatches WHERE OrganizationId = @OrganisationId";
        const string dataSql = """
            SELECT  Id                  AS BatchId
                ,   OrganizationId
                ,   FileName
                ,   FileType
                ,   FileHeaders
                ,   HeaderFingerprint
                ,   FileStoragePath
                ,   TotalRows
                ,   ImportedRows
                ,   SkippedRows
                ,   ErrorRows
                ,   Status
                ,   DuplicateStrategy
                ,   UploadedBy
                ,   UploadedAt
                ,   MappingSavedAt
                ,   ExecutionStartedAt
                ,   CompletedAt
                ,   Notes
            FROM    ImportBatches
            WHERE   OrganizationId = @OrganisationId
            ORDER BY UploadedAt DESC
            OFFSET  @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;
        var p = new { OrganisationId = organisationId, Offset = (page - 1) * pageSize, PageSize = pageSize };
        using var conn = _db.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<ImportBatch>(dataSql, p);
        return (items, total);
    }

    public async Task<ImportBatch?> GetBatchByIdAsync(Guid batchId)
    {
        const string sql = """
            SELECT  Id                  AS BatchId
                ,   OrganizationId
                ,   FileName
                ,   FileType
                ,   FileHeaders
                ,   HeaderFingerprint
                ,   FileStoragePath
                ,   TotalRows
                ,   ImportedRows
                ,   SkippedRows
                ,   ErrorRows
                ,   Status
                ,   DuplicateStrategy
                ,   UploadedBy
                ,   UploadedAt
                ,   MappingSavedAt
                ,   ExecutionStartedAt
                ,   CompletedAt
                ,   Notes
            FROM    ImportBatches
            WHERE   Id = @BatchId
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ImportBatch>(sql, new { BatchId = batchId });
    }

    public async Task<ImportBatch> CreateBatchAsync(ImportBatch batch)
    {
        batch.BatchId    = Guid.NewGuid();
        batch.UploadedAt = DateTime.UtcNow;

        const string sql = """
            INSERT INTO ImportBatches
                (Id, OrganizationId, FileName, FileType, FileHeaders, HeaderFingerprint, FileStoragePath,
                 TotalRows, Status, DuplicateStrategy, UploadedBy, UploadedAt)
            VALUES
                (@BatchId, @OrganizationId, @FileName, @FileType, @FileHeaders, @HeaderFingerprint, @FileStoragePath,
                 @TotalRows, @Status, @DuplicateStrategy, @UploadedBy, @UploadedAt)
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, batch);
        return batch;
    }

    public async Task<bool> UpdateBatchAsync(ImportBatch batch)
    {
        const string sql = """
            UPDATE  ImportBatches
            SET     Status              = @Status
                ,   TotalRows           = @TotalRows
                ,   ImportedRows        = @ImportedRows
                ,   SkippedRows         = @SkippedRows
                ,   ErrorRows           = @ErrorRows
                ,   MappingSavedAt      = @MappingSavedAt
                ,   ExecutionStartedAt  = @ExecutionStartedAt
                ,   CompletedAt         = @CompletedAt
                ,   DuplicateStrategy   = @DuplicateStrategy
                ,   Notes               = @Notes
            WHERE   Id = @BatchId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, batch);
        return rows > 0;
    }

    public async Task<IEnumerable<ImportColumnMapping>> GetMappingsByBatchIdAsync(Guid batchId)
    {
        const string sql = """
            SELECT  Id                  AS MappingId
                ,   ImportBatchId
                ,   CsvHeader
                ,   CsvColumnIndex
                ,   MappingType
                ,   CustomerFieldName
                ,   FieldDefinitionId
                ,   IsAutoMatched
                ,   IsRequired
                ,   SavedForReuse
                ,   DisplayOrder
            FROM    ImportColumnMappings
            WHERE   ImportBatchId = @BatchId
            ORDER BY DisplayOrder, CsvColumnIndex
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ImportColumnMapping>(sql, new { BatchId = batchId });
    }

    public async Task SaveMappingsAsync(Guid batchId, IEnumerable<ImportColumnMapping> mappings)
    {
        const string deleteSql = "DELETE FROM ImportColumnMappings WHERE ImportBatchId = @BatchId";
        const string insertSql = """
            INSERT INTO ImportColumnMappings
                (Id, ImportBatchId, CsvHeader, CsvColumnIndex, MappingType, CustomerFieldName,
                 FieldDefinitionId, IsAutoMatched, IsRequired, SavedForReuse, DisplayOrder)
            VALUES
                (NEWID(), @ImportBatchId, @CsvHeader, @CsvColumnIndex, @MappingType, @CustomerFieldName,
                 @FieldDefinitionId, @IsAutoMatched, @IsRequired, @SavedForReuse, @DisplayOrder)
            """;
        using var conn = (System.Data.Common.DbConnection)_db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync(deleteSql, new { BatchId = batchId }, tx);
        foreach (var m in mappings)
        {
            m.ImportBatchId = batchId;
            await conn.ExecuteAsync(insertSql, m, tx);
        }
        tx.Commit();
    }

    public async Task AddErrorAsync(ImportError error)
    {
        error.ErrorId   = Guid.NewGuid();
        error.CreatedAt = DateTime.UtcNow;

        const string sql = """
            INSERT INTO ImportErrors (Id, ImportBatchId, RowNumber, RawData, ErrorType, ErrorMessage, CreatedAt)
            VALUES (@ErrorId, @ImportBatchId, @RowNumber, @RawData, @ErrorType, @ErrorMessage, @CreatedAt)
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, error);
    }

    public async Task<IEnumerable<ImportError>> GetErrorsByBatchIdAsync(Guid batchId)
    {
        const string sql = """
            SELECT  Id              AS ErrorId
                ,   ImportBatchId
                ,   RowNumber
                ,   RawData
                ,   ErrorType
                ,   ErrorMessage
                ,   CreatedAt
            FROM    ImportErrors
            WHERE   ImportBatchId = @BatchId
            ORDER BY RowNumber
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ImportError>(sql, new { BatchId = batchId });
    }

    public async Task<IEnumerable<SavedColumnMapping>> GetSavedMappingsAsync(Guid organisationId, string fingerprint)
    {
        const string sql = """
            SELECT  Id                  AS SavedMappingId
                ,   OrganizationId
                ,   HeaderFingerprint
                ,   CsvHeader
                ,   CsvColumnIndex
                ,   MappingType
                ,   CustomerFieldName
                ,   FieldDefinitionId
                ,   DisplayOrder
                ,   LastUsedAt
                ,   UseCount
            FROM    SavedColumnMappings
            WHERE   OrganizationId      = @OrganisationId
              AND   HeaderFingerprint   = @Fingerprint
            ORDER BY DisplayOrder
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<SavedColumnMapping>(sql, new { OrganisationId = organisationId, Fingerprint = fingerprint });
    }

    public async Task SaveColumnMappingsAsync(Guid organisationId, string fingerprint, IEnumerable<SavedColumnMapping> mappings)
    {
        const string mergeSql = """
            MERGE SavedColumnMappings AS target
            USING (SELECT @OrganisationId AS OrganizationId, @Fingerprint AS HeaderFingerprint, @CsvHeader AS CsvHeader) AS src
                ON target.OrganizationId = src.OrganizationId
               AND target.HeaderFingerprint = src.HeaderFingerprint
               AND target.CsvHeader = src.CsvHeader
            WHEN MATCHED THEN
                UPDATE SET MappingType      = @MappingType
                        , CustomerFieldName = @CustomerFieldName
                        , FieldDefinitionId = @FieldDefinitionId
                        , DisplayOrder      = @DisplayOrder
                        , LastUsedAt        = SYSUTCDATETIME()
                        , UseCount          = target.UseCount + 1
            WHEN NOT MATCHED THEN
                INSERT (Id, OrganizationId, HeaderFingerprint, CsvHeader, CsvColumnIndex, MappingType, CustomerFieldName, FieldDefinitionId, DisplayOrder, LastUsedAt, UseCount)
                VALUES (NEWID(), @OrganisationId, @Fingerprint, @CsvHeader, @CsvColumnIndex, @MappingType, @CustomerFieldName, @FieldDefinitionId, @DisplayOrder, SYSUTCDATETIME(), 1);
            """;
        using var conn = (System.Data.Common.DbConnection)_db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        foreach (var m in mappings)
        {
            await conn.ExecuteAsync(mergeSql, new
            {
                OrganisationId  = organisationId,
                Fingerprint     = fingerprint,
                m.CsvHeader,
                m.CsvColumnIndex,
                m.MappingType,
                m.CustomerFieldName,
                m.FieldDefinitionId,
                m.DisplayOrder
            }, tx);
        }
        tx.Commit();
    }
}
