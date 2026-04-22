using POC.CustomerValidation.API.Extensions;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class FieldDefinitionService(IFieldDefinitionRepository fieldRepo, IFieldOptionRepository optionRepo, IFieldSectionRepository sectionRepo) : IFieldDefinitionService
{
    private readonly IFieldDefinitionRepository _fieldRepo = fieldRepo;
    private readonly IFieldOptionRepository _optionRepo = optionRepo;
    private readonly IFieldSectionRepository _sectionRepo = sectionRepo;



    public async Task<IEnumerable<FieldDefinitionDto>> GetByOrganizationIdAsync(Guid organizationId, bool includeInactive = false)
    {
        var fields = await _fieldRepo.GetByOrganizationIdAsync(organizationId, includeInactive);
        var result = new List<FieldDefinitionDto>();

        foreach (var field in fields)
        {
            var opts = IsOptionsField(field.FieldType) ? await _optionRepo.GetByFieldIdAsync(field.FieldDefinitionId) : [];
            result.Add(Map(field, null, opts));
        }
        return result;
    }

    public async Task<FieldDefinitionDto?> GetByIdAsync(Guid fieldDefinitionId)
    {
        var field = await _fieldRepo.GetByIdAsync(fieldDefinitionId);
        if (field is null) return null;

        var opts = IsOptionsField(field.FieldType) ? await _optionRepo.GetByFieldIdAsync(field.FieldDefinitionId) : [];

        FieldSection? section = await _sectionRepo.GetByIdAsync(field.FieldSectionId);

        return Map(field, section, opts);
    }

    public async Task<FieldDefinitionDto> CreateAsync(CreateFieldDefinitionRequest request)
    {
        var existing = await _fieldRepo.GetByKeyAsync(request.OrganizationId, request.FieldKey);
        if (existing is not null) throw new InvalidOperationException($"Field key '{request.FieldKey}' already exists for this organization.");
        
        var entity = new FieldDefinition
        {
            FieldDefinitionId   = Guid.NewGuid(),
            OrganizationId      = request.OrganizationId,
            FieldSectionId      = request.SectionId ?? Guid.Empty,
            FieldKey            = request.FieldKey,
            FieldLabel          = request.FieldLabel,
            FieldType           = request.FieldType,
            Placeholder         = request.PlaceholderText,
            HelpText            = request.HelpText,
            IsRequired          = request.IsRequired,
            IsActive            = true,
            DisplayOrder        = request.DisplayOrder,
            MinValue            = request.MinValue,
            MaxValue            = request.MaxValue,
            MinLength           = request.MinLength,
            MaxLength           = request.MaxLength,
            RegexPattern        = request.RegexPattern,
            CreatedDt           = DateTime.UtcNow,
            ModifiedDt          = DateTime.UtcNow
        };

        await _fieldRepo.CreateAsync(entity);

        return Map(entity, null, []);
    }

    public async Task<FieldDefinitionDto> UpdateAsync(UpdateFieldDefinitionRequest request)
    {
        var entity = await _fieldRepo.GetByIdAsync(request.FieldDefinitionId) ?? throw new KeyNotFoundException("Field definition not found.");

        entity.UpdateFromRequest(request);
        await _fieldRepo.UpdateAsync(entity);
        var opts = IsOptionsField(entity.FieldType) ? await _optionRepo.GetByFieldIdAsync(entity.FieldDefinitionId) : [];

        return Map(entity, null, opts);

    }

    public async Task ReorderAsync(Guid organizationId, IEnumerable<(Guid fieldDefinitionId, int displayOrder)> updates)
    {
        var ok = await _fieldRepo.ReorderAsync(updates);
        if (!ok) throw new InvalidOperationException("Failed to reorder field definitions. Please ensure all provided IDs are valid.");
    }
    

    public async Task SetStatusAsync(Guid fieldDefinitionId, bool isActive)
    {
        var ok = await _fieldRepo.ChangeStatusAsync(fieldDefinitionId, isActive);
        if (!ok) throw new KeyNotFoundException("Field definition not found.");
    }




    private static FieldDefinitionDto Map(FieldDefinition entity, FieldSection? section, IEnumerable<FieldOption> opts)
    {
        return new FieldDefinitionDto(
            FieldDefinitionId: entity.FieldDefinitionId,
            OrganizationId: entity.OrganizationId,
            SectionId: entity.FieldSectionId,
            SectionName: section?.SectionName,
            FieldKey: entity.FieldKey,
            FieldLabel: entity.FieldLabel,
            FieldType: entity.FieldType,
            PlaceholderText: entity.Placeholder,
            HelpText: entity.HelpText,
            IsRequired: entity.IsRequired,
            IsActive: entity.IsActive,
            DisplayOrder: entity.DisplayOrder,
            MinValue: entity.MinValue,
            MaxValue: entity.MaxValue,
            MinLength: entity.MinLength,
            MaxLength: entity.MaxLength,
            RegexPattern: entity.RegexPattern,
            Options: opts.Select(o => new FieldOptionDto(o.OptionId, o.FieldDefinitionId, o.OptionKey, o.OptionLabel, o.DisplayOrder, o.IsActive))
        );
    }


    private static bool IsOptionsField(string fieldType)
        => fieldType is "dropdown" or "multiselect";





}
