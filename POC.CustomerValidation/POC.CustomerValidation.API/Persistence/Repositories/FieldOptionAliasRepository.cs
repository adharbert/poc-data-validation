using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class FieldOptionAliasRepository(IDbConnectionFactory db) : IFieldOptionAliasRepository
{
    private readonly IDbConnectionFactory _db = db;

    public async Task<IEnumerable<FieldOptionAlias>> GetByOrganizationAsync(Guid organizationId)
    {
        const string sql = """
            SELECT  Id, OrganizationId, FieldDefinitionId, AliasValue, CanonicalValue, CreatedDt, ModifiedDt
            FROM    FieldOptionAliases
            WHERE   OrganizationId = @OrganizationId
            ORDER BY FieldDefinitionId, AliasValue
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldOptionAlias>(sql, new { OrganizationId = organizationId });
    }

    public async Task<IEnumerable<FieldOptionAlias>> GetByOrganizationAndFieldsAsync(
        Guid organizationId, IEnumerable<Guid> fieldDefinitionIds)
    {
        var ids = fieldDefinitionIds.ToList();
        if (ids.Count == 0) return [];

        const string sql = """
            SELECT  Id, OrganizationId, FieldDefinitionId, AliasValue, CanonicalValue, CreatedDt, ModifiedDt
            FROM    FieldOptionAliases
            WHERE   OrganizationId    = @OrganizationId
            AND     FieldDefinitionId IN @Ids
            ORDER BY FieldDefinitionId, AliasValue
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldOptionAlias>(sql, new { OrganizationId = organizationId, Ids = ids });
    }

    public async Task BulkUpsertAsync(IEnumerable<FieldOptionAlias> aliases)
    {
        const string sql = """
            MERGE FieldOptionAliases AS target
            USING (SELECT @OrganizationId    AS OrganizationId,
                          @FieldDefinitionId AS FieldDefinitionId,
                          @AliasValue        AS AliasValue,
                          @CanonicalValue    AS CanonicalValue) AS source
                ON  target.OrganizationId    = source.OrganizationId
                AND target.FieldDefinitionId = source.FieldDefinitionId
                AND target.AliasValue        = source.AliasValue
            WHEN MATCHED THEN
                UPDATE SET
                    target.CanonicalValue = source.CanonicalValue,
                    target.ModifiedDt     = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, OrganizationId, FieldDefinitionId, AliasValue, CanonicalValue, CreatedDt, ModifiedDt)
                VALUES (NEWID(), source.OrganizationId, source.FieldDefinitionId, source.AliasValue, source.CanonicalValue, GETUTCDATE(), GETUTCDATE());
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, aliases);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM FieldOptionAliases WHERE Id = @Id";
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}
