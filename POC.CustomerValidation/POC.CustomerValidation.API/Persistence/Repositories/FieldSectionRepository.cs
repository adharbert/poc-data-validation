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
                SELECT Id	AS SectionId
                       , OrganizationId	
                       , SectionName	
                       , DisplayOrder
                       , IsActive
                FROM   FieldSections
                WHERE  OrganizationId = @FieldSectionId
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<FieldSection>(sql , new { FieldSectionId = fieldSectionId });
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
        fieldSection.FieldSectionId = Guid.NewGuid();

        const string sql = """
                INSERT INTO FieldSections (Id, OrganizationId, SectionName, DisplayOrder, IsActive)
                VALUES (@FieldSectionId, @OrganizationId, @SectionName, @DisplayOrder, @IsActive)
            """;
        using var conn = _db.CreateConnection();
        await conn.QueryAsync<FieldSection>(sql , fieldSection);
        return fieldSection.FieldSectionId;
    }


    public async Task<bool> UpdateAsync(FieldSection fieldSection)
    {
        const string sql = """
                UPDATE FieldSections
                SET    SectionName = @SectionName
                       , DisplayOrder = @DisplayOrder
                       , IsActive = @IsActive
                WHERE  Id = @FieldSectionId
            """;
        using var conn = _db.CreateConnection();
        var rowsAffected = await conn.ExecuteAsync(sql, fieldSection);
        return rowsAffected > 0;
    }


    public async Task<bool> DeleteAsync(Guid sectionId)
    {
        const string sql = "DELETE FROM FieldSections WHERE  Id = @SectionId";
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<FieldSection>(sql, new { SectionId = sectionId });
        return rows.Any();
    }
}
