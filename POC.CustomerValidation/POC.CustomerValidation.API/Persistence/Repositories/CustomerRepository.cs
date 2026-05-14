using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class CustomerRepository(IDbConnectionFactory db) : ICustomerRepository
{
    private readonly IDbConnectionFactory _db = db;

    private const string SelectColumns = """
        SELECT  Id              AS CustomerId
            ,   OrganizationId
            ,   FirstName
            ,   LastName
            ,   MiddleName
            ,   MaidenName
            ,   DateOfBirth
            ,   CustomerCode
            ,   OriginalId
            ,   Email
            ,   Phone
            ,   IsActive
            ,   CreatedDt       AS CreatedDate
            ,   ModifiedDt      AS ModifiedDate
        FROM    Customers
        """;

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetByOrganisationIdAsync(
        Guid organisationId, bool includeInactive = false, int page = 1, int pageSize = 50)
    {
        const string countSql = """
            SELECT COUNT(1)
            FROM   Customers
            WHERE  OrganizationId   = @OrganisationId
              AND  (@IncludeInactive = 1 OR IsActive = 1)
            """;

        const string dataSql = """
            SELECT  Id              AS CustomerId
                ,   OrganizationId
                ,   FirstName
                ,   LastName
                ,   MiddleName
                ,   MaidenName
                ,   DateOfBirth
                ,   CustomerCode
                ,   OriginalId
                ,   Email
                ,   Phone
                ,   IsActive
                ,   CreatedDt       AS CreatedDate
                ,   ModifiedDt      AS ModifiedDate
            FROM    Customers
            WHERE   OrganizationId   = @OrganisationId
              AND   (@IncludeInactive = 1 OR IsActive = 1)
            ORDER BY LastName, FirstName
            OFFSET  @Offset ROWS
            FETCH   NEXT @PageSize ROWS ONLY
            """;

        var param = new { OrganisationId = organisationId, IncludeInactive = includeInactive, Offset = (page - 1) * pageSize, PageSize = pageSize };
        using var conn = _db.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var items = await conn.QueryAsync<Customer>(dataSql, param);
        return (items, total);
    }

    public async Task<Customer?> GetByIdAsync(Guid customerId)
    {
        const string sql = SelectColumns + " WHERE Id = @CustomerId";
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Customer>(sql, new { CustomerId = customerId });
    }

    public async Task<Customer?> GetByEmailAsync(Guid organisationId, string email)
    {
        const string sql = SelectColumns + " WHERE OrganizationId = @OrganisationId AND Email = @Email AND IsActive = 1";
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Customer>(sql, new { OrganisationId = organisationId, Email = email });
    }

    public async Task<Customer?> GetByOriginalIdAsync(Guid organisationId, string originalId)
    {
        const string sql = SelectColumns + " WHERE OrganizationId = @OrganisationId AND OriginalId = @OriginalId AND IsActive = 1";
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Customer>(sql, new { OrganisationId = organisationId, OriginalId = originalId });
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        customer.CustomerId  = Guid.NewGuid();
        customer.CreatedDate = DateTime.UtcNow;
        customer.ModifiedDate = DateTime.UtcNow;

        const string sql = """
            INSERT INTO Customers (Id, OrganizationId, FirstName, LastName, MiddleName, MaidenName, DateOfBirth, CustomerCode, OriginalId, Email, Phone, IsActive, CreatedDt, ModifiedDt)
            VALUES (@CustomerId, @OrganizationId, @FirstName, @LastName, @MiddleName, @MaidenName, @DateOfBirth, @CustomerCode, @OriginalId, @Email, @Phone, @IsActive, @CreatedDate, @ModifiedDate)
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, customer);
        return customer;
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        customer.ModifiedDate = DateTime.UtcNow;

        const string sql = """
            UPDATE  Customers
            SET     FirstName       = @FirstName
                ,   LastName        = @LastName
                ,   MiddleName      = @MiddleName
                ,   MaidenName      = @MaidenName
                ,   DateOfBirth     = @DateOfBirth
                ,   OriginalId      = @OriginalId
                ,   Email           = @Email
                ,   Phone           = @Phone
                ,   IsActive        = @IsActive
                ,   ModifiedDt      = @ModifiedDate
            WHERE   Id = @CustomerId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, customer);
        return rows > 0;
    }

    public async Task<bool> ChangeStatusAsync(Guid customerId, bool isActive)
    {
        const string sql = """
            UPDATE  Customers
            SET     IsActive    = @IsActive
                ,   ModifiedDt  = @ModifiedDt
            WHERE   Id = @CustomerId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { CustomerId = customerId, IsActive = isActive, ModifiedDt = DateTime.UtcNow });
        return rows > 0;
    }
}
