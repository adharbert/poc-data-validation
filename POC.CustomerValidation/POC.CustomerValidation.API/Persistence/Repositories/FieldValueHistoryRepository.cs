using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class FieldValueHistoryRepository(IDbConnectionFactory db) : IFieldValueHistoryRepository
{
    private readonly IDbConnectionFactory _db = db;



    public async Task<IEnumerable<FieldValueHistory>> GetByValueIdAsync(Guid valueId)
    {
        const string sql = """
            SELECT	Id      AS HistoryId
            		, FieldValueId
            		, CustomerId
            		, FieldDefinitionId
            		, ValueText
            		, ValueNumber
            		, ValueDate
            		, ValueDatetime
            		, ValueBoolean
            		, ChangeBy
            		, ChangeAt
            		, ChangeReason
            FROM	FieldValuesHistory
            WHERE	FieldValueId = @ValueId
            ORDER   BY ChangeAt DESC
            """;

        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldValueHistory>(sql, new { ValueId = valueId });
    }

    public async Task<IEnumerable<FieldValueHistory>> GetByCustomerIdAsync(Guid customerId, int page = 1, int pageSize = 50)
    {
        const string sql = """
            SELECT	Id      AS HistoryId
            		, FieldValueId
            		, CustomerId
            		, FieldDefinitionId
            		, ValueText
            		, ValueNumber
            		, ValueDate
            		, ValueDatetime
            		, ValueBoolean
            		, ChangeBy
            		, ChangeAt
            		, ChangeReason
            FROM	FieldValuesHistory
            WHERE	CustomerId = @CustomerId
            ORDER   BY ChangeAt DESC
            OFFSET  @Offset ROWS
            FETCH   NEXT @PageSize ROWS ONLY
            """;

        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldValueHistory>(sql, new
        {
            CustomerId  = customerId,
            Offset      = (page - 1) * pageSize,
            PageSize    = pageSize
        });
    }

    public async Task<IEnumerable<FieldValueHistory>> GetByFieldIdAsync(Guid customerId, Guid fieldDefinitionId)
    {
        const string sql = """
            SELECT	Id      AS HistoryId
            		, FieldValueId
            		, CustomerId
            		, FieldDefinitionId
            		, ValueText
            		, ValueNumber
            		, ValueDate
            		, ValueDatetime
            		, ValueBoolean
            		, ChangeBy
            		, ChangeAt
            		, ChangeReason
            FROM	FieldValuesHistory
            WHERE	CustomerId = @CustomerId
                    AND FieldDefinitionId = @FieldDefinitionId  
            ORDER   BY ChangeAt DESC
        """;

        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldValueHistory>(sql, new { CustomerId = customerId, FieldDefinitionId = fieldDefinitionId });
    }
}
