using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class CustomerAddressRepository(IDbConnectionFactory db) : ICustomerAddressRepository
{
    private readonly IDbConnectionFactory _db = db;

    private const string SelectColumns = """
        SELECT  Id              AS AddressId
            ,   CustomerId
            ,   AddressLine1
            ,   AddressLine2
            ,   City
            ,   State
            ,   PostalCode
            ,   Country
            ,   MelissaValidated
            ,   CustomerConfirmed
            ,   IsCurrent
            ,   CreatedUtcDt
            ,   ModifiedUtcDt
        FROM    CustomerAddresses
        """;

    public async Task<IEnumerable<CustomerAddress>> GetByCustomerIdAsync(Guid customerId)
    {
        const string sql = SelectColumns + """
             WHERE  CustomerId = @CustomerId
             ORDER BY CreatedUtcDt DESC
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<CustomerAddress>(sql, new { CustomerId = customerId });
    }

    public async Task<CustomerAddress?> GetCurrentAsync(Guid customerId)
    {
        const string sql = SelectColumns + """
             WHERE  CustomerId = @CustomerId
               AND  IsCurrent  = 1
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<CustomerAddress>(sql, new { CustomerId = customerId });
    }

    public async Task<CustomerAddress?> GetByIdAsync(Guid addressId)
    {
        const string sql = SelectColumns + " WHERE Id = @AddressId";
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<CustomerAddress>(sql, new { AddressId = addressId });
    }

    public async Task<CustomerAddress> CreateAsync(CustomerAddress address)
    {
        address.AddressId    = Guid.NewGuid();
        address.IsCurrent    = true;
        address.CreatedUtcDt  = DateTime.UtcNow;
        address.ModifiedUtcDt = DateTime.UtcNow;

        const string clearCurrentSql = """
            UPDATE  CustomerAddresses
            SET     IsCurrent     = 0
                ,   ModifiedUtcDt = @Now
            WHERE   CustomerId = @CustomerId
              AND   IsCurrent  = 1
            """;

        const string insertSql = """
            INSERT INTO CustomerAddresses
                (Id, CustomerId, AddressLine1, AddressLine2, City, State, PostalCode, Country,
                 MelissaValidated, CustomerConfirmed, IsCurrent, CreatedUtcDt, ModifiedUtcDt)
            VALUES
                (@AddressId, @CustomerId, @AddressLine1, @AddressLine2, @City, @State, @PostalCode, @Country,
                 @MelissaValidated, @CustomerConfirmed, @IsCurrent, @CreatedUtcDt, @ModifiedUtcDt)
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(clearCurrentSql, new { CustomerId = address.CustomerId, Now = address.CreatedUtcDt });
        await conn.ExecuteAsync(insertSql, address);
        return address;
    }

    public async Task<bool> ConfirmAsync(Guid addressId)
    {
        const string sql = """
            UPDATE  CustomerAddresses
            SET     CustomerConfirmed = 1
                ,   ModifiedUtcDt     = @Now
            WHERE   Id = @AddressId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { AddressId = addressId, Now = DateTime.UtcNow });
        return rows > 0;
    }
}
