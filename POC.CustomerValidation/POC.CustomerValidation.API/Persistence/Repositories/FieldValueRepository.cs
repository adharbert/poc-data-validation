using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class FieldValueRepository(IDbConnectionFactory db) : IFieldValueRepository
{
    private readonly IDbConnectionFactory _db = db;



    public async Task<IEnumerable<FieldValue>> GetByCustomerIdAsync(Guid customerId)
    {
        const string sql = """
            SELECT	fv.Id as FieldValueId
            		, fv.CustomerId
            		, fv.FieldDefinitionId
            		, fv.ValueText
            		, fv.ValueNumber
            		, fv.ValueDate
            		, fv.ValueDatetime
            		, fv.ValueBoolean
            		, fv.ConfirmedAt
            		, fv.ConfirmedBy
            		, fv.FlaggedAt
            		, fv.FlagNote
            		, fv.CreatedDt
            		, fv.ModifiedDt
            FROM	FieldValues fv
            WHERE	fv.CustomerId = @CustomerId
            """;

        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldValue>(sql, new { CustomerId = customerId });
    }


    public async Task<FieldValue?> GetByCustomerAndFieldAsync(Guid customerId, Guid FieldDescriptionId)
    {
        const string sql = """
            SELECT	fv.Id as FieldValueId
            		, fv.CustomerId
            		, fv.FieldDefinitionId
            		, fv.ValueText
            		, fv.ValueNumber
            		, fv.ValueDate
            		, fv.ValueDatetime
            		, fv.ValueBoolean
            		, fv.ConfirmedAt
            		, fv.ConfirmedBy
            		, fv.FlaggedAt
            		, fv.FlagNote
            		, fv.CreatedDt
            		, fv.ModifiedDt
            FROM	FieldValues fv
            WHERE	fv.CustomerId = @CustomerId
                    AND fv.FieldDefinitionId = @FieldDescriptionId  
            """;

        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<FieldValue?>(sql, new { CustomerId = customerId, FieldDescriptionId });
    }


    public async Task<Guid> UpsertAsync(FieldValue fieldValue)
    {
        fieldValue.ModifiedDt = DateTime.UtcNow;

        const string sql = """
            MERGE FieldValues AS target
            USING (SELECT @CustomerId, @FieldDefinitionId, @ValueText, @ValueNumber, @ValueDate, @ValueDatetime, @ValueBoolean, @ConfirmedAt, @ConfirmedBy, @FlaggedAt, @FlagNote) AS source
                ON target.CustomerId = @CustomerId AND target.FieldDefinitionId = @FieldDefinitionId
            WHEN MATCHED THEN
                target.ValueText        = @ValueText
                , target.ValueNumber    = @ValueNumber
                , target.ValueDate      = @ValueDate
                , target.ValueDatetime  = @ValueDatetime
                , target.ValueBoolean   = @ValueBoolean
                , target.ConfirmedAt    = @ConfirmedAt
                , target.ConfirmedBy    = @ConfirmedBy
                , target.FlaggedAt      = @FlaggedAt
                , target.FlagNote       = @FlagNote
                , target.ModifiedDt     = @ModifiedDt
            WHEN NOT MATCHED THEN
                INSERT (FieldValueId,   CustomerId,     FieldDefinitionId,  ValueText,  ValueNumber,    ValueDate,  ValueDatetime,  ValueBoolean,   ConfirmedAt,    ConfirmedBy,    FlaggedAt,  FlagNote,   CreatedDt,      ModifiedDt)
                VALUES (NEWID(),        @CustomerId,    @FieldDefinitionId, @ValueText, @ValueNumber,   @ValueDate, @ValueDatetime, @ValueBoolean,  @ConfirmedAt,   @ConfirmedBy,   @FlaggedAt, @FlagNote,  GETUTCDATE(),   @ModifiedDt)
            OUTPUT inserted.FieldValueId;
            """;

        using var conn = _db.CreateConnection();
        var newId = await conn.ExecuteScalarAsync<Guid>(sql, fieldValue);
        return newId == Guid.Empty ? fieldValue.FieldValueId : newId;
    }


    public async Task<bool> ConfirmAsync(Guid valueId, string confirmedBy)
    {
        const string sql = """
            UPDATE FieldValues
            SET ConfirmedAt = GETUTCDATE(),
                ConfirmedBy = @ConfirmedBy,
                ModifiedDt  = GETUTCDATE()
            WHERE Id = @ValueId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { ValueId = valueId, ConfirmedBy = confirmedBy });
        return rows > 0;
    }

    public async Task<bool> FlagAsync(Guid valueId, string FlagNote)
    {
        const string sql = """
            UPDATE FieldValues
            SET FlaggedAt   = GETUTCDATE(),
                FlagNote    = @FlagNote,
                ModifiedDt  = GETUTCDATE()
            WHERE Id = @ValueId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { ValueId = valueId, FlagNote });
        return rows > 0;
    }
}
