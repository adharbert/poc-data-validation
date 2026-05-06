using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class LibraryRepository(IDbConnectionFactory db, ILogger<LibraryRepository> logger) : ILibraryRepository
{
    private readonly IDbConnectionFactory _db     = db;
    private readonly ILogger<LibraryRepository> _logger = logger;

    // -------------------------------------------------------
    // Sections
    // -------------------------------------------------------

    public async Task<IEnumerable<LibrarySection>> GetAllSectionsAsync(bool includeInactive = false)
    {
        const string sql = """
            SELECT  Id, SectionName, Description, DisplayOrder, IsActive, CreatedDt, ModifiedDt
            FROM    dbo.LibrarySections
            WHERE   (@IncludeInactive = 1 OR IsActive = 1)
            ORDER BY DisplayOrder, SectionName
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<LibrarySection>(sql, new { IncludeInactive = includeInactive ? 1 : 0 });
    }

    public async Task<LibrarySection?> GetSectionByIdAsync(Guid sectionId)
    {
        const string sql = """
            SELECT  Id, SectionName, Description, DisplayOrder, IsActive, CreatedDt, ModifiedDt
            FROM    dbo.LibrarySections
            WHERE   Id = @SectionId
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<LibrarySection>(sql, new { SectionId = sectionId });
    }

    public async Task<Guid> CreateSectionAsync(LibrarySection section)
    {
        section.Id = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.LibrarySections (Id, SectionName, Description, DisplayOrder, IsActive)
            VALUES (@Id, @SectionName, @Description, @DisplayOrder, @IsActive)
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, section);
        return section.Id;
    }

    public async Task<bool> UpdateSectionAsync(LibrarySection section)
    {
        const string sql = """
            UPDATE dbo.LibrarySections
            SET    SectionName  = @SectionName,
                   Description  = @Description,
                   DisplayOrder = @DisplayOrder,
                   IsActive     = @IsActive,
                   ModifiedDt   = GETUTCDATE()
            WHERE  Id = @Id
            """;
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(sql, section) > 0;
    }

    public async Task<bool> SetSectionStatusAsync(Guid sectionId, bool isActive)
    {
        const string sql = "UPDATE dbo.LibrarySections SET IsActive = @IsActive, ModifiedDt = GETUTCDATE() WHERE Id = @SectionId";
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(sql, new { SectionId = sectionId, IsActive = isActive }) > 0;
    }

    // -------------------------------------------------------
    // Fields
    // -------------------------------------------------------

    public async Task<IEnumerable<LibraryField>> GetAllFieldsAsync(bool includeInactive = false)
    {
        const string sql = """
            SELECT  Id, FieldKey, FieldLabel, FieldType, PlaceHolderText, HelpText,
                    IsRequired, DisplayOrder, MinValue, MaxValue, MinLength, MaxLength,
                    RegExPattern, DisplayFormat, IsActive, CreatedDt, ModifiedDt
            FROM    dbo.LibraryFields
            WHERE   (@IncludeInactive = 1 OR IsActive = 1)
            ORDER BY DisplayOrder, FieldLabel
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<LibraryField>(sql, new { IncludeInactive = includeInactive ? 1 : 0 });
    }

    public async Task<LibraryField?> GetFieldByIdAsync(Guid fieldId)
    {
        const string sql = """
            SELECT  Id, FieldKey, FieldLabel, FieldType, PlaceHolderText, HelpText,
                    IsRequired, DisplayOrder, MinValue, MaxValue, MinLength, MaxLength,
                    RegExPattern, DisplayFormat, IsActive, CreatedDt, ModifiedDt
            FROM    dbo.LibraryFields
            WHERE   Id = @FieldId
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<LibraryField>(sql, new { FieldId = fieldId });
    }

    public async Task<Guid> CreateFieldAsync(LibraryField field)
    {
        field.Id = Guid.NewGuid();
        const string sql = """
            INSERT INTO dbo.LibraryFields
                (Id, FieldKey, FieldLabel, FieldType, PlaceHolderText, HelpText,
                 IsRequired, DisplayOrder, MinValue, MaxValue, MinLength, MaxLength,
                 RegExPattern, DisplayFormat, IsActive)
            VALUES
                (@Id, @FieldKey, @FieldLabel, @FieldType, @PlaceHolderText, @HelpText,
                 @IsRequired, @DisplayOrder, @MinValue, @MaxValue, @MinLength, @MaxLength,
                 @RegExPattern, @DisplayFormat, @IsActive)
            """;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, field);
        return field.Id;
    }

    public async Task<bool> UpdateFieldAsync(LibraryField field)
    {
        const string sql = """
            UPDATE dbo.LibraryFields
            SET    FieldLabel      = @FieldLabel,
                   FieldType       = @FieldType,
                   PlaceHolderText = @PlaceHolderText,
                   HelpText        = @HelpText,
                   IsRequired      = @IsRequired,
                   DisplayOrder    = @DisplayOrder,
                   MinValue        = @MinValue,
                   MaxValue        = @MaxValue,
                   MinLength       = @MinLength,
                   MaxLength       = @MaxLength,
                   RegExPattern    = @RegExPattern,
                   DisplayFormat   = @DisplayFormat,
                   IsActive        = @IsActive,
                   ModifiedDt      = GETUTCDATE()
            WHERE  Id = @Id
            """;
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(sql, field) > 0;
    }

    public async Task<bool> SetFieldStatusAsync(Guid fieldId, bool isActive)
    {
        const string sql = "UPDATE dbo.LibraryFields SET IsActive = @IsActive, ModifiedDt = GETUTCDATE() WHERE Id = @FieldId";
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync(sql, new { FieldId = fieldId, IsActive = isActive }) > 0;
    }

    // -------------------------------------------------------
    // Options
    // -------------------------------------------------------

    public async Task<IEnumerable<LibraryFieldOption>> GetOptionsByFieldIdAsync(Guid fieldId)
    {
        const string sql = """
            SELECT  Id, LibraryFieldId, OptionKey, OptionLabel, DisplayOrder, IsActive
            FROM    dbo.LibraryFieldOptions
            WHERE   LibraryFieldId = @FieldId AND IsActive = 1
            ORDER BY DisplayOrder
            """;
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<LibraryFieldOption>(sql, new { FieldId = fieldId });
    }

    public async Task BulkUpsertOptionsAsync(Guid fieldId, IEnumerable<LibraryFieldOption> options)
    {
        const string sql = """
            MERGE dbo.LibraryFieldOptions AS target
            USING (
                SELECT  @LibraryFieldId AS LibraryFieldId,
                        @OptionKey      AS OptionKey,
                        @OptionLabel    AS OptionLabel,
                        @DisplayOrder   AS DisplayOrder
            ) AS source
                ON target.LibraryFieldId = source.LibraryFieldId AND target.OptionKey = source.OptionKey
            WHEN MATCHED THEN
                UPDATE SET OptionLabel = source.OptionLabel, DisplayOrder = source.DisplayOrder, IsActive = 1
            WHEN NOT MATCHED THEN
                INSERT (Id, LibraryFieldId, OptionKey, OptionLabel, DisplayOrder, IsActive)
                VALUES (NEWID(), source.LibraryFieldId, source.OptionKey, source.OptionLabel, source.DisplayOrder, 1);
            """;

        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var opt in options)
                await conn.ExecuteAsync(sql, new
                {
                    LibraryFieldId = fieldId,
                    opt.OptionKey,
                    opt.OptionLabel,
                    opt.DisplayOrder,
                }, tx);
            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error during bulk upsert of library field options for FieldId: {FieldId}", fieldId);
            throw;
        }
        finally { conn.Close(); }
    }

    // -------------------------------------------------------
    // Section ↔ Field assignments
    // -------------------------------------------------------

    public async Task AssignFieldsToSectionAsync(Guid sectionId, IEnumerable<(Guid FieldId, int DisplayOrder)> assignments)
    {
        const string deleteSql = "DELETE FROM dbo.LibrarySectionFields WHERE LibrarySectionId = @SectionId";
        const string insertSql = """
            INSERT INTO dbo.LibrarySectionFields (LibrarySectionId, LibraryFieldId, DisplayOrder)
            VALUES (@SectionId, @FieldId, @DisplayOrder)
            """;

        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(deleteSql, new { SectionId = sectionId }, tx);
            foreach (var (fieldId, order) in assignments)
                await conn.ExecuteAsync(insertSql, new { SectionId = sectionId, FieldId = fieldId, DisplayOrder = order }, tx);
            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error assigning fields to library section {SectionId}", sectionId);
            throw;
        }
        finally { conn.Close(); }
    }

    // -------------------------------------------------------
    // Bulk load for import picker
    // -------------------------------------------------------

    public async Task<IEnumerable<(LibrarySection Section, IEnumerable<(LibraryField Field, IEnumerable<LibraryFieldOption> Options)> Fields)>> GetSectionsWithFieldsAsync(IEnumerable<Guid> sectionIds)
    {
        var ids = sectionIds.ToList();
        if (ids.Count == 0) return [];

        const string sectionSql = """
            SELECT  Id, SectionName, Description, DisplayOrder, IsActive, CreatedDt, ModifiedDt
            FROM    dbo.LibrarySections
            WHERE   Id IN @Ids AND IsActive = 1
            ORDER BY DisplayOrder
            """;

        const string fieldSql = """
            SELECT  f.Id, sf.LibrarySectionId AS SectionId,
                    f.FieldKey, f.FieldLabel, f.FieldType, f.PlaceHolderText, f.HelpText,
                    f.IsRequired, sf.DisplayOrder, f.MinValue, f.MaxValue, f.MinLength, f.MaxLength,
                    f.RegExPattern, f.DisplayFormat, f.IsActive, f.CreatedDt, f.ModifiedDt
            FROM    dbo.LibrarySectionFields sf
            JOIN    dbo.LibraryFields f ON f.Id = sf.LibraryFieldId
            WHERE   sf.LibrarySectionId IN @Ids AND f.IsActive = 1
            ORDER BY sf.LibrarySectionId, sf.DisplayOrder
            """;

        const string optionSql = """
            SELECT  o.Id, o.LibraryFieldId, o.OptionKey, o.OptionLabel, o.DisplayOrder, o.IsActive
            FROM    dbo.LibraryFieldOptions o
            JOIN    dbo.LibrarySectionFields sf ON sf.LibraryFieldId = o.LibraryFieldId
            WHERE   sf.LibrarySectionId IN @Ids AND o.IsActive = 1
            ORDER BY o.LibraryFieldId, o.DisplayOrder
            """;

        using var conn = _db.CreateConnection();

        var sections   = (await conn.QueryAsync<LibrarySection>(sectionSql, new { Ids = ids })).ToList();
        var fieldRows  = (await conn.QueryAsync<FieldWithSection>(fieldSql, new { Ids = ids })).ToList();
        var allOptions = (await conn.QueryAsync<LibraryFieldOption>(optionSql, new { Ids = ids })).ToList();

        var optsByField = allOptions
            .GroupBy(o => o.LibraryFieldId)
            .ToDictionary(g => g.Key, g => (IEnumerable<LibraryFieldOption>)g.ToList());

        var fieldsBySection = fieldRows
            .GroupBy(r => r.SectionId)
            .ToDictionary(
                g => g.Key,
                g => (IEnumerable<(LibraryField, IEnumerable<LibraryFieldOption>)>)
                    g.Select(r =>
                    {
                        var field = r.ToLibraryField();
                        var opts = optsByField.TryGetValue(field.Id, out var o) ? o : [];
                        return (field, opts);
                    }).ToList()
            );

        return sections.Select(s => (
            s,
            fieldsBySection.TryGetValue(s.Id, out var fields)
                ? fields
                : Enumerable.Empty<(LibraryField, IEnumerable<LibraryFieldOption>)>()
        ));
    }

    // Flat projection used by GetSectionsWithFieldsAsync to avoid Dapper tuple split-on issues
    private class FieldWithSection
    {
        public Guid     Id              { get; set; }
        public Guid     SectionId       { get; set; }
        public string   FieldKey        { get; set; } = default!;
        public string   FieldLabel      { get; set; } = default!;
        public string   FieldType       { get; set; } = default!;
        public string?  PlaceHolderText { get; set; }
        public string?  HelpText        { get; set; }
        public bool     IsRequired      { get; set; }
        public int      DisplayOrder    { get; set; }
        public decimal? MinValue        { get; set; }
        public decimal? MaxValue        { get; set; }
        public int?     MinLength       { get; set; }
        public int?     MaxLength       { get; set; }
        public string?  RegExPattern    { get; set; }
        public string?  DisplayFormat   { get; set; }
        public bool     IsActive        { get; set; }
        public DateTime CreatedDt       { get; set; }
        public DateTime ModifiedDt      { get; set; }

        public LibraryField ToLibraryField() => new()
        {
            Id = Id, FieldKey = FieldKey, FieldLabel = FieldLabel, FieldType = FieldType,
            PlaceHolderText = PlaceHolderText, HelpText = HelpText, IsRequired = IsRequired,
            DisplayOrder = DisplayOrder, MinValue = MinValue, MaxValue = MaxValue,
            MinLength = MinLength, MaxLength = MaxLength, RegExPattern = RegExPattern,
            DisplayFormat = DisplayFormat, IsActive = IsActive,
            CreatedDt = CreatedDt, ModifiedDt = ModifiedDt,
        };
    }
}
