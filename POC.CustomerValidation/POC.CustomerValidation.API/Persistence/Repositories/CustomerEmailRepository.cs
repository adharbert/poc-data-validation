using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class CustomerEmailRepository(IDbConnectionFactory db) : ICustomerEmailRepository
{
    public async Task<IEnumerable<CustomerEmail>> GetByCustomerIdAsync(Guid customerId)
    {
        const string sql = """
            SELECT  Id              AS EmailId
                ,   CustomerId
                ,   EmailAddress
                ,   EmailType
                ,   IsPrimary
                ,   IsActive
                ,   CreatedUtcDt
                ,   ModifiedUtcDt
            FROM    CustomerEmails
            WHERE   CustomerId = @CustomerId
            ORDER BY IsPrimary DESC, CreatedUtcDt
            """;
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<CustomerEmail>(sql, new { CustomerId = customerId });
    }

    public async Task<CustomerEmail> CreateAsync(CustomerEmail email)
    {
        email.EmailId       = Guid.NewGuid();
        email.CreatedUtcDt  = DateTime.UtcNow;
        email.ModifiedUtcDt = DateTime.UtcNow;

        const string sql = """
            INSERT INTO CustomerEmails (Id, CustomerId, EmailAddress, EmailType, IsPrimary, IsActive, CreatedUtcDt, ModifiedUtcDt)
            VALUES (@EmailId, @CustomerId, @EmailAddress, @EmailType, @IsPrimary, @IsActive, @CreatedUtcDt, @ModifiedUtcDt)
            """;
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(sql, email);
        return email;
    }

    public async Task<bool> UpdateAsync(CustomerEmail email)
    {
        email.ModifiedUtcDt = DateTime.UtcNow;

        const string sql = """
            UPDATE  CustomerEmails
            SET     EmailAddress    = @EmailAddress
                ,   EmailType       = @EmailType
                ,   IsPrimary       = @IsPrimary
                ,   IsActive        = @IsActive
                ,   ModifiedUtcDt   = @ModifiedUtcDt
            WHERE   Id = @EmailId
            """;
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, email);
        return rows > 0;
    }

    public async Task<bool> ChangeStatusAsync(Guid emailId, bool isActive)
    {
        const string sql = """
            UPDATE  CustomerEmails
            SET     IsActive        = @IsActive
                ,   ModifiedUtcDt   = @ModifiedUtcDt
            WHERE   Id = @EmailId
            """;
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { EmailId = emailId, IsActive = isActive, ModifiedUtcDt = DateTime.UtcNow });
        return rows > 0;
    }
}
