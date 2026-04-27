using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class ImportColumnStagingRepository(IDbConnectionFactory db) : IImportColumnStagingRepository
{
    private readonly IDbConnectionFactory _db = db;

    private const string SelectColumns = """
        SELECT  Id                  AS StagingId
            ,   OrganizationId
            ,   CsvHeader
            ,   HeaderNormalized
            ,   Status
            ,   MappingType
            ,   CustomerFieldName
            ,   FieldDefinitionId
            ,   FirstSeenAt
            ,   LastSeenAt
            ,   SeenCount
            ,   ResolvedAt
            ,   ResolvedBy
            ,   Notes
        FROM    ImportColumnStaging
        """;

    public async Task<IEnumerable<ImportColumnStaging>> GetByOrganisationIdAsync(Guid organisationId, string? status = null)
    {
        const string sql = """
            SELECT  Id                  AS StagingId
                ,   OrganizationId
                ,   CsvHeader
                ,   HeaderNormalized
                ,   Status
                ,   MappingType
                ,   CustomerFieldName
                ,   FieldDefinitionId
                ,   FirstSeenAt
                ,   LastSeenAt
                ,   SeenCount
                ,   ResolvedAt
                ,   ResolvedBy
                ,   Notes
            FROM    ImportColumnStaging
            WHERE   OrganizationId  = @OrganisationId
              AND   (@Status IS NULL OR Status = @Status)
            ORDER BY Status, LastSeenAt DESC
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ImportColumnStaging>(sql, new { OrganisationId = organisationId, Status = status });
    }

    public async Task<ImportColumnStaging?> GetByIdAsync(Guid stagingId)
    {
        const string sql = SelectColumns + " WHERE Id = @StagingId";
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ImportColumnStaging>(sql, new { StagingId = stagingId });
    }

    public async Task<ImportColumnStaging?> GetByHeaderAsync(Guid organisationId, string headerNormalized)
    {
        const string sql = SelectColumns + " WHERE OrganizationId = @OrganisationId AND HeaderNormalized = @HeaderNormalized";
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ImportColumnStaging>(sql, new { OrganisationId = organisationId, HeaderNormalized = headerNormalized });
    }

    public async Task<ImportColumnStaging> CreateAsync(ImportColumnStaging staging)
    {
        staging.StagingId   = Guid.NewGuid();
        staging.FirstSeenAt = DateTime.UtcNow;
        staging.LastSeenAt  = DateTime.UtcNow;

        const string sql = """
            INSERT INTO ImportColumnStaging
                (Id, OrganizationId, CsvHeader, HeaderNormalized, Status, MappingType, CustomerFieldName, FieldDefinitionId,
                 FirstSeenAt, LastSeenAt, SeenCount, Notes)
            VALUES
                (@StagingId, @OrganizationId, @CsvHeader, @HeaderNormalized, @Status, @MappingType, @CustomerFieldName, @FieldDefinitionId,
                 @FirstSeenAt, @LastSeenAt, @SeenCount, @Notes)
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, staging);
        return staging;
    }

    public async Task<bool> UpdateAsync(ImportColumnStaging staging)
    {
        const string sql = """
            UPDATE  ImportColumnStaging
            SET     Status              = @Status
                ,   MappingType         = @MappingType
                ,   CustomerFieldName   = @CustomerFieldName
                ,   FieldDefinitionId   = @FieldDefinitionId
                ,   LastSeenAt          = @LastSeenAt
                ,   SeenCount           = @SeenCount
                ,   ResolvedAt          = @ResolvedAt
                ,   ResolvedBy          = @ResolvedBy
                ,   Notes               = @Notes
            WHERE   Id = @StagingId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, staging);
        return rows > 0;
    }

    public async Task TouchAsync(Guid stagingId)
    {
        const string sql = """
            UPDATE  ImportColumnStaging
            SET     LastSeenAt  = SYSUTCDATETIME()
                ,   SeenCount   = SeenCount + 1
            WHERE   Id = @StagingId
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, new { StagingId = stagingId });
    }

    public async Task<bool> DeleteAsync(Guid stagingId)
    {
        const string sql = "DELETE FROM ImportColumnStaging WHERE Id = @StagingId";
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { StagingId = stagingId });
        return rows > 0;
    }
}
