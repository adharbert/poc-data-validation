using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class LibraryService(
    ILibraryRepository libraryRepo,
    IFieldSectionRepository sectionRepo,
    IFieldDefinitionRepository fieldRepo,
    IFieldOptionRepository optionRepo) : ILibraryService
{
    private readonly ILibraryRepository         _libraryRepo = libraryRepo;
    private readonly IFieldSectionRepository    _sectionRepo = sectionRepo;
    private readonly IFieldDefinitionRepository _fieldRepo   = fieldRepo;
    private readonly IFieldOptionRepository     _optionRepo  = optionRepo;

    // -------------------------------------------------------
    // Sections
    // -------------------------------------------------------

    public async Task<IEnumerable<LibrarySectionDto>> GetAllSectionsAsync(bool includeInactive = false)
    {
        var sections = await _libraryRepo.GetAllSectionsAsync(includeInactive);
        var allFields = await _libraryRepo.GetAllFieldsAsync(includeInactive);

        var fieldMap = allFields.ToDictionary(f => f.Id);
        var results = new List<LibrarySectionDto>();

        foreach (var section in sections)
        {
            var sectionDetail = await _libraryRepo.GetSectionByIdAsync(section.Id);
            results.Add(await MapSectionAsync(section));
        }

        return results;
    }

    public async Task<LibrarySectionDto?> GetSectionByIdAsync(Guid sectionId)
    {
        var section = await _libraryRepo.GetSectionByIdAsync(sectionId);
        if (section is null) return null;
        return await MapSectionAsync(section);
    }

    public async Task<LibrarySectionDto> CreateSectionAsync(CreateLibrarySectionRequest request)
    {
        var entity = new LibrarySection
        {
            SectionName  = request.SectionName,
            Description  = request.Description,
            DisplayOrder = request.DisplayOrder,
            IsActive     = true,
        };
        var id = await _libraryRepo.CreateSectionAsync(entity);
        entity.Id = id;
        return MapSection(entity, []);
    }

    public async Task<LibrarySectionDto> UpdateSectionAsync(Guid sectionId, UpdateLibrarySectionRequest request)
    {
        var entity = await _libraryRepo.GetSectionByIdAsync(sectionId)
            ?? throw new KeyNotFoundException($"Library section {sectionId} not found.");

        entity.SectionName  = request.SectionName;
        entity.Description  = request.Description;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive     = request.IsActive;

        await _libraryRepo.UpdateSectionAsync(entity);
        return await MapSectionAsync(entity);
    }

    public async Task SetSectionStatusAsync(Guid sectionId, bool isActive)
        => await _libraryRepo.SetSectionStatusAsync(sectionId, isActive);

    public async Task AssignFieldsToSectionAsync(Guid sectionId, AssignLibraryFieldsRequest request)
    {
        var assignments = request.Fields.Select(f => (f.LibraryFieldId, f.DisplayOrder));
        await _libraryRepo.AssignFieldsToSectionAsync(sectionId, assignments);
    }

    // -------------------------------------------------------
    // Fields
    // -------------------------------------------------------

    public async Task<IEnumerable<LibraryFieldDto>> GetAllFieldsAsync(bool includeInactive = false)
    {
        var fields = await _libraryRepo.GetAllFieldsAsync(includeInactive);
        var result = new List<LibraryFieldDto>();
        foreach (var f in fields)
            result.Add(await MapFieldAsync(f));
        return result;
    }

    public async Task<LibraryFieldDto?> GetFieldByIdAsync(Guid fieldId)
    {
        var field = await _libraryRepo.GetFieldByIdAsync(fieldId);
        if (field is null) return null;
        return await MapFieldAsync(field);
    }

    public async Task<LibraryFieldDto> CreateFieldAsync(CreateLibraryFieldRequest request)
    {
        var entity = new LibraryField
        {
            FieldKey        = request.FieldKey,
            FieldLabel      = request.FieldLabel,
            FieldType       = request.FieldType,
            PlaceHolderText = request.PlaceHolderText,
            HelpText        = request.HelpText,
            IsRequired      = request.IsRequired,
            DisplayOrder    = request.DisplayOrder,
            MinValue        = request.MinValue,
            MaxValue        = request.MaxValue,
            MinLength       = request.MinLength,
            MaxLength       = request.MaxLength,
            RegExPattern    = request.RegExPattern,
            DisplayFormat   = request.DisplayFormat,
            IsActive        = true,
        };
        var id = await _libraryRepo.CreateFieldAsync(entity);
        entity.Id = id;
        return MapField(entity, []);
    }

    public async Task<LibraryFieldDto> UpdateFieldAsync(Guid fieldId, UpdateLibraryFieldRequest request)
    {
        var entity = await _libraryRepo.GetFieldByIdAsync(fieldId)
            ?? throw new KeyNotFoundException($"Library field {fieldId} not found.");

        entity.FieldLabel      = request.FieldLabel;
        entity.FieldType       = request.FieldType;
        entity.PlaceHolderText = request.PlaceHolderText;
        entity.HelpText        = request.HelpText;
        entity.IsRequired      = request.IsRequired;
        entity.DisplayOrder    = request.DisplayOrder;
        entity.MinValue        = request.MinValue;
        entity.MaxValue        = request.MaxValue;
        entity.MinLength       = request.MinLength;
        entity.MaxLength       = request.MaxLength;
        entity.RegExPattern    = request.RegExPattern;
        entity.DisplayFormat   = request.DisplayFormat;
        entity.IsActive        = request.IsActive;

        await _libraryRepo.UpdateFieldAsync(entity);
        return await MapFieldAsync(entity);
    }

    public async Task SetFieldStatusAsync(Guid fieldId, bool isActive)
        => await _libraryRepo.SetFieldStatusAsync(fieldId, isActive);

    public async Task BulkUpsertOptionsAsync(Guid fieldId, BulkUpsertFieldOptionsRequest request)
    {
        var options = request.Options.Select((o, i) => new LibraryFieldOption
        {
            LibraryFieldId = fieldId,
            OptionKey      = o.OptionKey.Trim().ToLowerInvariant(),
            OptionLabel    = o.OptionLabel.Trim(),
            DisplayOrder   = o.DisplayOrder == 0 ? i + 1 : o.DisplayOrder,
            IsActive       = true,
        });
        await _libraryRepo.BulkUpsertOptionsAsync(fieldId, options);
    }

    // -------------------------------------------------------
    // Import to org
    // -------------------------------------------------------

    public async Task<ImportFromLibraryResult> ImportToOrgAsync(ImportFromLibraryRequest request)
    {
        var sectionData = (await _libraryRepo.GetSectionsWithFieldsAsync(request.SectionIds)).ToList();

        int sectionsCreated = 0, fieldsCreated = 0, optionsCreated = 0;

        // Calculate starting display order for sections in this org
        var existingSections = await _sectionRepo.GetByOrganizationIdAsync(request.OrganizationId);
        int nextSectionOrder = existingSections.Any()
            ? existingSections.Max(s => s.DisplayOrder) + 1
            : 1;

        foreach (var (section, fieldEntries) in sectionData)
        {
            var newSection = new FieldSection
            {
                OrganizationId = request.OrganizationId,
                SectionName    = section.SectionName,
                DisplayOrder   = nextSectionOrder++,
                IsActive       = true,
            };
            var newSectionId = await _sectionRepo.CreateAsync(newSection);
            sectionsCreated++;

            // Calculate starting display order for fields in this section
            var existingFields = await _fieldRepo.GetBySectionIdAsync(newSectionId);
            int nextFieldOrder = existingFields.Any()
                ? existingFields.Max(f => f.DisplayOrder) + 1
                : 1;

            foreach (var (libField, libOptions) in fieldEntries)
            {
                // Append a short unique suffix so re-importing the same section doesn't collide on FieldKey
                var suffix   = Guid.NewGuid().ToString("N")[..6];
                var fieldKey = $"{libField.FieldKey}_{suffix}";

                var newField = new FieldDefinition
                {
                    OrganizationId  = request.OrganizationId,
                    FieldSectionId  = newSectionId,
                    FieldKey        = fieldKey,
                    FieldLabel      = libField.FieldLabel,
                    FieldType       = libField.FieldType,
                    Placeholder     = libField.PlaceHolderText,
                    HelpText        = libField.HelpText,
                    IsRequired      = libField.IsRequired,
                    DisplayOrder    = nextFieldOrder++,
                    MinValue        = libField.MinValue,
                    MaxValue        = libField.MaxValue,
                    MinLength       = libField.MinLength,
                    MaxLength       = libField.MaxLength,
                    RegexPattern    = libField.RegExPattern,
                    DisplayFormat   = libField.DisplayFormat,
                    IsActive        = true,
                };
                var newFieldId = await _fieldRepo.CreateAsync(newField);
                fieldsCreated++;

                var optionEntities = libOptions.Select((o, i) => new FieldOption
                {
                    FieldDefinitionId = newFieldId,
                    OptionKey         = o.OptionKey,
                    OptionLabel       = o.OptionLabel,
                    DisplayOrder      = o.DisplayOrder == 0 ? i + 1 : o.DisplayOrder,
                    IsActive          = true,
                }).ToList();

                foreach (var opt in optionEntities)
                {
                    await _optionRepo.CreateAsync(opt);
                    optionsCreated++;
                }
            }
        }

        return new ImportFromLibraryResult
        {
            SectionsCreated = sectionsCreated,
            FieldsCreated   = fieldsCreated,
            OptionsCreated  = optionsCreated,
        };
    }

    // -------------------------------------------------------
    // Mapping helpers
    // -------------------------------------------------------

    private async Task<LibrarySectionDto> MapSectionAsync(LibrarySection section)
    {
        // Fetch fields for this section via the section↔field junction
        var sectionData = (await _libraryRepo.GetSectionsWithFieldsAsync([section.Id])).FirstOrDefault();
        var fields = sectionData == default
            ? Enumerable.Empty<LibraryFieldDto>()
            : sectionData.Fields.Select(f => MapField(f.Field, f.Options));
        return MapSection(section, fields);
    }

    private async Task<LibraryFieldDto> MapFieldAsync(LibraryField field)
    {
        var options = await _libraryRepo.GetOptionsByFieldIdAsync(field.Id);
        return MapField(field, options);
    }

    private static LibrarySectionDto MapSection(LibrarySection s, IEnumerable<LibraryFieldDto> fields) =>
        new()
        {
            Id           = s.Id,
            SectionName  = s.SectionName,
            Description  = s.Description,
            DisplayOrder = s.DisplayOrder,
            IsActive     = s.IsActive,
            Fields       = fields,
        };

    private static LibraryFieldDto MapField(LibraryField f, IEnumerable<LibraryFieldOption> options) =>
        new()
        {
            Id              = f.Id,
            FieldKey        = f.FieldKey,
            FieldLabel      = f.FieldLabel,
            FieldType       = f.FieldType,
            PlaceHolderText = f.PlaceHolderText,
            HelpText        = f.HelpText,
            IsRequired      = f.IsRequired,
            DisplayOrder    = f.DisplayOrder,
            MinValue        = f.MinValue,
            MaxValue        = f.MaxValue,
            MinLength       = f.MinLength,
            MaxLength       = f.MaxLength,
            RegExPattern    = f.RegExPattern,
            DisplayFormat   = f.DisplayFormat,
            IsActive        = f.IsActive,
            Options         = options.Select(o => new LibraryFieldOptionDto
            {
                Id             = o.Id,
                LibraryFieldId = o.LibraryFieldId,
                OptionKey      = o.OptionKey,
                OptionLabel    = o.OptionLabel,
                DisplayOrder   = o.DisplayOrder,
                IsActive       = o.IsActive,
            }),
        };
}
