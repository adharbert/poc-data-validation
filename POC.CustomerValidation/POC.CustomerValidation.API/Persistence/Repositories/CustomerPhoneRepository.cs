using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class CustomerPhoneRepository(IDbConnectionFactory db) : ICustomerPhoneRepository
{
    public async Task<IEnumerable<CustomerPhone>> GetByCustomerIdAsync(Guid customerId)
    {
        const string sql = """
            SELECT  Id              AS PhoneId
                ,   CustomerId
                ,   PhoneNumber
                ,   PhoneType
                ,   IsPrimary
                ,   IsActive
                ,   CreatedUtcDt
                ,   ModifiedUtcDt
            FROM    CustomerPhones
            WHERE   CustomerId = @CustomerId
            ORDER BY IsPrimary DESC, CreatedUtcDt
            """;
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<CustomerPhone>(sql, new { CustomerId = customerId });
    }

    public async Task<CustomerPhone> CreateAsync(CustomerPhone phone)
    {
        phone.PhoneId       = Guid.NewGuid();
        phone.CreatedUtcDt  = DateTime.UtcNow;
        phone.ModifiedUtcDt = DateTime.UtcNow;

        const string sql = """
            INSERT INTO CustomerPhones (Id, CustomerId, PhoneNumber, PhoneType, IsPrimary, IsActive, CreatedUtcDt, ModifiedUtcDt)
            VALUES (@PhoneId, @CustomerId, @PhoneNumber, @PhoneType, @IsPrimary, @IsActive, @CreatedUtcDt, @ModifiedUtcDt)
            """;
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(sql, phone);
        return phone;
    }

    public async Task<bool> UpdateAsync(CustomerPhone phone)
    {
        phone.ModifiedUtcDt = DateTime.UtcNow;

        const string sql = """
            UPDATE  CustomerPhones
            SET     PhoneNumber     = @PhoneNumber
                ,   PhoneType       = @PhoneType
                ,   IsPrimary       = @IsPrimary
                ,   IsActive        = @IsActive
                ,   ModifiedUtcDt   = @ModifiedUtcDt
            WHERE   Id = @PhoneId
            """;
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, phone);
        return rows > 0;
    }

    public async Task<bool> ChangeStatusAsync(Guid phoneId, bool isActive)
    {
        const string sql = """
            UPDATE  CustomerPhones
            SET     IsActive        = @IsActive
                ,   ModifiedUtcDt   = @ModifiedUtcDt
            WHERE   Id = @PhoneId
            """;
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { PhoneId = phoneId, IsActive = isActive, ModifiedUtcDt = DateTime.UtcNow });
        return rows > 0;
    }
}
