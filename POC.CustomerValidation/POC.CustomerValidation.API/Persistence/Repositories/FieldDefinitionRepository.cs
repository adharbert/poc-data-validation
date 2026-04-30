using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class FieldDefinitionRepository(IDbConnectionFactory db) : IFieldDefinitionRepository
{
    private readonly IDbConnectionFactory _db = db;
    private const string SelectColumns = """
                fd.Id as FieldDefinitionId
        		, fd.OrganizationId
        		, fd.FieldSectionId
        		, fd.FieldKey
        		, fd.FieldLabel
        		, fd.FieldType
        		, fd.PlaceHolderText
        		, fd.HelpText
        		, fd.IsRequired
        		, fd.IsActive
        		, fd.DisplayOrder
        		, fd.MinValue
        		, fd.MaxValue
        		, fd.MinLength
        		, fd.MaxLength
        		, fd.RegExPattern
        		, fd.DisplayFormat
        		, fd.CreatedDt
        		, fd.ModifiedDt
        """;



    public async Task<IEnumerable<FieldDefinition>> GetByOrganizationIdAsync(Guid organizationId, bool includeInactive = false)
    {
        var sql = $"""
                SELECT {SelectColumns}
                FROM   FieldDefinitions fd with(nolock)
                WHERE  fd.OrganizationId = @OrganizationId 
                        AND (@IncludeInactive = 1 OR fd.IsActive = 1)    
                ORDER   BY fd.FieldSectionId, fd.DisplayOrder
        """;
        using var connection = _db.CreateConnection();
        return await connection.QueryAsync<FieldDefinition>(sql, new { OrganizationId = organizationId, IncludeInactive = includeInactive });
    }


    public async Task<FieldDefinition?> GetByIdAsync(Guid FieldDefinitionId)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM   FieldDefinitions fd with(nolock)
            WHERE  fd.Id = @FieldDefinitionId
            """;
        using var connection = _db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<FieldDefinition?>(sql, new { FieldDefinitionId });

    }


    public async Task<FieldDefinition?> GetByKeyAsync(Guid organizationId, string fieldKey)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM   FieldDefinitions fd with(nolock)
            WHERE  fd.OrganizationId = @OrganizationId 
                    AND fd.FieldKey = @FieldKey
            """;
        using var connection = _db.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<FieldDefinition?>(sql, new { OrganizationId = organizationId, FieldKey = fieldKey });
    }

    public async Task<IEnumerable<FieldDefinition>> GetBySectionIdAsync(Guid sectionId)
    {
        var sql = $"""
                SELECT {SelectColumns}
                FROM   FieldDefinitions fd with(nolock)
                WHERE  fd.FieldSectionId = @SectionId
                ORDER   BY fd.DisplayOrder
        """;
        using var connection = _db.CreateConnection();
        return await connection.QueryAsync<FieldDefinition>(sql, new { SectionId = sectionId });
    }

    public async Task<Guid> CreateAsync(FieldDefinition fieldDefinition)
    {
        fieldDefinition.FieldDefinitionId = Guid.NewGuid();
        fieldDefinition.CreatedDt = DateTime.UtcNow;
        fieldDefinition.ModifiedDt = DateTime.UtcNow;

        const string sql = """
                INSERT INTO FieldDefinitions (Id, OrganizationId, FieldSectionId, FieldKey, FieldLabel, FieldType, PlaceHolderText, HelpText, IsRequired, IsActive, DisplayOrder, MinValue, MaxValue, MinLength, MaxLength, RegExPattern, DisplayFormat, CreatedDt, ModifiedDt)
                VALUES (@FieldDefinitionId, @OrganizationId, @FieldSectionId, @FieldKey, @FieldLabel, @FieldType, @Placeholder, @HelpText, @IsRequired, @IsActive, @DisplayOrder, @MinValue, @MaxValue, @MinLength, @MaxLength, @RegexPattern, @DisplayFormat, @CreatedDt, @ModifiedDt)
            """;
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(sql, fieldDefinition);
        return fieldDefinition.FieldDefinitionId;
    }

    public async Task<bool> UpdateAsync(FieldDefinition fieldDefinition)
    {
        fieldDefinition.ModifiedDt = DateTime.UtcNow;

        const string sql = """
                UPDATE FieldDefinitions
                SET    OrganizationId   = @OrganizationId,
                       FieldSectionId   = @FieldSectionId,
                       FieldKey         = @FieldKey,
                       FieldLabel       = @FieldLabel,
                       FieldType        = @FieldType,
                       PlaceHolderText  = @Placeholder,
                       HelpText         = @HelpText,
                       IsRequired       = @IsRequired,
                       IsActive         = @IsActive,
                       DisplayOrder     = @DisplayOrder,
                       MinValue         = @MinValue,
                       MaxValue         = @MaxValue,
                       MinLength        = @MinLength,
                       MaxLength        = @MaxLength,
                       RegExPattern     = @RegexPattern,
                       DisplayFormat    = @DisplayFormat,
                       ModifiedDt       = @ModifiedDt
                WHERE  Id = @FieldDefinitionId
            """;

        using var connection = _db.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, fieldDefinition);
        return rows > 0;
    }

    public async Task<bool> ReorderAsync(IEnumerable<(Guid FieldDefinitionId, int DisplayOrder)> updates)
    {
        const string sql = """
                UPDATE FieldDefinitions
                SET    DisplayOrder = @DisplayOrder
                       , ModifiedDt = @ModifiedDt 
                WHERE  Id = @FieldDefinitionId
            """;
        
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, updates.Select(u => new { u.FieldDefinitionId, u.DisplayOrder, ModifiedDt = DateTime.UtcNow }));
        return rows > 0;
    }

    public async Task<bool> ChangeStatusAsync(Guid FieldDefinitionId, bool IsActive)
    {
        const string sql = """
                UPDATE FieldDefinitions
                SET    IsActive = @IsActive,
                       ModifiedDt = @ModifiedDt
                WHERE  Id = @FieldDefinitionId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { FieldDefinitionId, IsActive, ModifiedDt = DateTime.UtcNow });
        return rows > 0;
    }

    public async Task<bool> BulkAssignSectionAsync(Guid? sectionId, IEnumerable<(Guid FieldId, int DisplayOrder)> assignments)
    {
        const string sql = """
                UPDATE FieldDefinitions
                SET    FieldSectionId = @SectionId,
                       DisplayOrder   = @DisplayOrder,
                       ModifiedDt     = @ModifiedDt
                WHERE  Id = @FieldId
            """;
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(sql,
            assignments.Select(a => new { SectionId = sectionId, a.DisplayOrder, FieldId = a.FieldId, ModifiedDt = DateTime.UtcNow }));
        return rows > 0;
    }

    public async Task<IEnumerable<FieldPreviewRaw>> GetPreviewAsync(Guid organizationId, Guid customerId)
    {
        const string sql = """
                SELECT  fd.Id                                           AS FieldDefinitionId
                    ,   fd.FieldSectionId                               AS SectionId
                    ,   fs.SectionName
                    ,   ISNULL(fs.DisplayOrder, 9999)                   AS SectionDisplayOrder
                    ,   fd.FieldKey
                    ,   fd.FieldLabel
                    ,   fd.FieldType
                    ,   fd.HelpText
                    ,   fd.IsRequired
                    ,   fd.DisplayOrder
                    ,   fd.DisplayFormat
                    ,   fv.ValueText
                    ,   fv.ValueNumber
                    ,   fv.ValueDate
                    ,   fv.ValueBoolean
                FROM    FieldDefinitions fd WITH(NOLOCK)
                LEFT JOIN FieldSections  fs WITH(NOLOCK) ON fs.Id = fd.FieldSectionId
                LEFT JOIN FieldValues    fv WITH(NOLOCK) ON fv.FieldDefinitionId = fd.Id
                                                         AND fv.CustomerId = @CustomerId
                WHERE   fd.OrganizationId = @OrganizationId
                  AND   fd.IsActive = 1
                ORDER BY SectionDisplayOrder, fd.DisplayOrder
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<FieldPreviewRaw>(sql, new { OrganizationId = organizationId, CustomerId = customerId });
    }
}
