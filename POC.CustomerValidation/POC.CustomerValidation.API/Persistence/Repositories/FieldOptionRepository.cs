using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class FieldOptionRepository(IDbConnectionFactory db, ILogger<FieldOptionRepository> logger) : IFieldOptionRepository
{
    private readonly IDbConnectionFactory _db = db;   
    private readonly ILogger<FieldOptionRepository> _logger = logger;




    public async Task<FieldOption?> GetByIdAsync(Guid optionId)
    {
        const string sql = """
            Select	Id As OptionId
            		, FieldDefinitionId
            		, OptionKey
            		, OptionLabel
            		, DisplayOrder
            		, IsActive
            from	FieldOptions
            WHERE	Id = @OptionId
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<FieldOption>(sql, new { OptionId = optionId });

    }

    public async Task<IEnumerable<FieldOption>> GetByFieldIdAsync(Guid FieldDefinitionId, bool includeInactive = false)
    {
        const string sql = """
            Select	Id As OptionId
            		, FieldDefinitionId
            		, OptionKey
            		, OptionLabel
            		, DisplayOrder
            		, IsActive
            from	FieldOptions
            WHERE	FieldDefinitionId = @FieldDefinitionId
            		AND (@IncludeInactive = 1 OR IsActive = 1)
            """;

        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldOption>(sql, new { FieldDefinitionId, IncludeInactive = includeInactive ? 1 : 0 });
    }

    public async Task<Guid> CreateAsync(FieldOption option)
    {
        option.OptionId = Guid.NewGuid();

        const string sql = """
                INSERT INTO FieldOptions (Id, FieldDefinitionId, OptionKey, OptionLabel, DisplayOrder, IsActive)
                VALUES (@OptionId, @FieldDefinitionId, @OptionKey, @OptionLabel, @DisplayOrder, @IsActive)
            """;

        using var conn = _db.CreateConnection();
        await conn.QueryAsync<FieldOption>(sql, option);
        return option.OptionId;
    }

    public async Task BulkUpsertAsync(Guid FieldDefinitionId, IEnumerable<FieldOption> options)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();    // create transaction incase an error occurs.

        try
        {
            // SQL Merge statement.
            const string sql = """
                MERGE FieldOptions AS target
                USING (
                    SELECT
                        @FieldDefinitionId  AS FieldDefinitionId,
                        @OptionKey          AS OptionKey,
                        @OptionLabel        AS OptionLabel,
                        @DisplayOrder       AS DisplayOrder
                ) AS source
                    ON target.FieldDefinitionId = source.FieldDefinitionId AND target.OptionKey = source.OptionKey
                WHEN MATCHED THEN
                    UPDATE SET
                        OptionLabel     = source.OptionLabel,
                        DisplayOrder    = source.DisplayOrder,
                        IsActive        = 1
                WHEN NOT MATCHED THEN
                    INSERT (Id,     FieldDefinitionId,          OptionKey,          OptionLabel,            DisplayOrder,           IsActive)
                    VALUES (NEWID(), source.FieldDefinitionId,  source.OptionKey,   source.OptionLabel,     source.DisplayOrder,    1);
                """;
            // Loop through options and execute query after completed.
            foreach (var option in options) 
                await conn.ExecuteAsync(sql, new
                {
                    FieldDefinitionId,
                    option.OptionKey,
                    option.OptionLabel,
                    option.DisplayOrder
                }, transaction);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Error during bulk upsert of field options for FieldDefinitionId: {FieldDefinitionId}", FieldDefinitionId);
            throw;
        }
        finally
        {
            conn.Close();
            conn.Dispose();
        }
    }

    public async Task<bool> UpdateAsync(FieldOption option)
    {
        const string sql = """
                UPDATE FieldOptions
                SET FieldDefinitionId   = @FieldDefinitionId,
                    OptionKey           = @OptionKey,
                    OptionLabel         = @OptionLabel,
                    DisplayOrder        = @DisplayOrder,
                    IsActive            = @IsActive
                WHERE Id = @OptionId
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<FieldOption>(sql, option);
        return rows.Any();
    }

    public async Task<bool> DeleteAsync(Guid optionId)
    {
        const string sql = "DELETE FROM FieldOptions WHERE Id = @OptionId";
        using var conn = _db.CreateConnection();
        var roes = await conn.QueryAsync<FieldOption>(sql, new { OptionId = optionId });
        return roes.Any();
    }

}
