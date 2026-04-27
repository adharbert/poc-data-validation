using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class ContractRepository(IDbConnectionFactory db) : IContractRepository
{
    private readonly IDbConnectionFactory _db = db;

    public async Task<IEnumerable<Contract>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false)
    {
        const string sql = """
            SELECT  Id              AS ContractId
                ,   OrganizationId
                ,   ContractName
                ,   ContractNumber
                ,   StartDate
                ,   EndDate
                ,   IsActive
                ,   Notes
                ,   CreatedDt
                ,   CreatedBy
                ,   ModifiedDt
                ,   ModifiedBy
            FROM    Contracts
            WHERE   OrganizationId  = @OrganisationId
              AND   (@IncludeInactive = 1 OR IsActive = 1)
            ORDER BY IsActive DESC, StartDate DESC
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Contract>(sql, new { OrganisationId = organisationId, IncludeInactive = includeInactive });
    }

    public async Task<Contract?> GetByIdAsync(Guid contractId)
    {
        const string sql = """
            SELECT  Id              AS ContractId
                ,   OrganizationId
                ,   ContractName
                ,   ContractNumber
                ,   StartDate
                ,   EndDate
                ,   IsActive
                ,   Notes
                ,   CreatedDt
                ,   CreatedBy
                ,   ModifiedDt
                ,   ModifiedBy
            FROM    Contracts
            WHERE   Id = @ContractId
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Contract>(sql, new { ContractId = contractId });
    }

    public async Task<Contract?> GetActiveAsync(Guid organisationId)
    {
        const string sql = """
            SELECT  Id              AS ContractId
                ,   OrganizationId
                ,   ContractName
                ,   ContractNumber
                ,   StartDate
                ,   EndDate
                ,   IsActive
                ,   Notes
                ,   CreatedDt
                ,   CreatedBy
                ,   ModifiedDt
                ,   ModifiedBy
            FROM    Contracts
            WHERE   OrganizationId  = @OrganisationId
              AND   IsActive        = 1
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Contract>(sql, new { OrganisationId = organisationId });
    }

    public async Task<Contract> CreateAsync(Contract contract)
    {
        contract.ContractId = Guid.NewGuid();
        contract.CreatedDt  = DateTime.UtcNow;

        const string sql = """
            INSERT INTO Contracts (Id, OrganizationId, ContractName, ContractNumber, StartDate, EndDate, IsActive, Notes, CreatedDt, CreatedBy)
            VALUES (@ContractId, @OrganizationId, @ContractName, @ContractNumber, @StartDate, @EndDate, @IsActive, @Notes, @CreatedDt, @CreatedBy)
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, contract);
        return contract;
    }

    public async Task<bool> UpdateAsync(Contract contract)
    {
        contract.ModifiedDt = DateTime.UtcNow;

        const string sql = """
            UPDATE  Contracts
            SET     ContractName    = @ContractName
                ,   ContractNumber  = @ContractNumber
                ,   StartDate       = @StartDate
                ,   EndDate         = @EndDate
                ,   Notes           = @Notes
                ,   ModifiedDt      = @ModifiedDt
                ,   ModifiedBy      = @ModifiedBy
            WHERE   Id = @ContractId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, contract);
        return rows > 0;
    }

    public async Task<bool> ChangeStatusAsync(Guid contractId, bool isActive, string modifiedBy)
    {
        const string sql = """
            UPDATE  Contracts
            SET     IsActive    = @IsActive
                ,   ModifiedDt  = @ModifiedDt
                ,   ModifiedBy  = @ModifiedBy
            WHERE   Id = @ContractId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { ContractId = contractId, IsActive = isActive, ModifiedDt = DateTime.UtcNow, ModifiedBy = modifiedBy });
        return rows > 0;
    }
}
