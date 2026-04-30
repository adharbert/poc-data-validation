using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class FieldSectionRepository(IDbConnectionFactory db) : IFieldSectionRepository
{
    private readonly IDbConnectionFactory _db = db;



    public async Task<FieldSection?> GetByIdAsync(Guid fieldSectionId)
    {
        const string sql = """
                SELECT Id AS SectionId
                       , OrganizationId
                       , SectionName
                       , DisplayOrder
                       , IsActive
                FROM   FieldSections
                WHERE  Id = @FieldSectionId
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<FieldSection>(sql, new { FieldSectionId = fieldSectionId });
    }


    public async Task<IEnumerable<FieldSection>> GetByOrganizationIdAsync(Guid organizationId)
    {
        const string sql = """
                SELECT Id	AS SectionId
                       , OrganizationId	
                       , SectionName	
                       , DisplayOrder
                       , IsActive
                FROM   FieldSections
                WHERE  OrganizationId = @OrganizationId
                ORDER  BY DisplayOrder
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldSection>(sql , new { OrganizationId = organizationId });
    }


    public async Task<Guid> CreateAsync(FieldSection fieldSection)
    {
        fieldSection.SectionId = Guid.NewGuid();

        const string sql = """
                INSERT INTO FieldSections (Id, OrganizationId, SectionName, DisplayOrder, IsActive)
                VALUES (@SectionId, @OrganizationId, @SectionName, @DisplayOrder, @IsActive)
            """;
        using var conn = _db.CreateConnection();
        await conn.QueryAsync<FieldSection>(sql , fieldSection);
        return fieldSection.SectionId;
    }


    public async Task<bool> UpdateAsync(FieldSection fieldSection)
    {
        const string sql = """
                UPDATE FieldSections
                SET    SectionName = @SectionName
                       , DisplayOrder = @DisplayOrder
                       , IsActive = @IsActive
                WHERE  Id = @SectionId
            """;
        using var conn = _db.CreateConnection();
        var rowsAffected = await conn.ExecuteAsync(sql, fieldSection);
        return rowsAffected > 0;
    }


    public async Task<bool> DeleteAsync(Guid sectionId)
    {
        // Soft-delete: preserve for reporting history
        return await ChangeStatusAsync(sectionId, false);
    }

    public async Task<bool> ChangeStatusAsync(Guid sectionId, bool isActive)
    {
        const string sql = "UPDATE FieldSections SET IsActive = @IsActive WHERE Id = @SectionId";
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { SectionId = sectionId, IsActive = isActive });
        return rows > 0;
    }

    public async Task<bool> ReorderAsync(IEnumerable<(Guid SectionId, int DisplayOrder)> updates)
    {
        const string sql = "UPDATE FieldSections SET DisplayOrder = @DisplayOrder WHERE Id = @SectionId";
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, updates.Select(u => new { u.SectionId, u.DisplayOrder }));
        return rows > 0;
    }
}
