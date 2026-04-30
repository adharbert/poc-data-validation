using POC.CustomerValidation.API.Extensions;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class FieldDefinitionService(IFieldDefinitionRepository fieldRepo, IFieldOptionRepository optionRepo, IFieldSectionRepository sectionRepo, ICustomerRepository customerRepo) : IFieldDefinitionService
{
    private readonly IFieldDefinitionRepository _fieldRepo    = fieldRepo;
    private readonly IFieldOptionRepository     _optionRepo   = optionRepo;
    private readonly IFieldSectionRepository    _sectionRepo  = sectionRepo;
    private readonly ICustomerRepository        _customerRepo = customerRepo;



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

        FieldSection? section = field.FieldSectionId.HasValue
            ? await _sectionRepo.GetByIdAsync(field.FieldSectionId.Value)
            : null;

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
            FieldSectionId      = request.SectionId,
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
            DisplayFormat       = request.DisplayFormat,
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




    public async Task<CustomerFormPreviewDto> GetFormPreviewAsync(Guid organizationId, Guid customerId)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId)
            ?? throw new KeyNotFoundException("Customer not found.");

        var rows    = (await _fieldRepo.GetPreviewAsync(organizationId, customerId)).ToList();
        var allOpts = new Dictionary<Guid, IEnumerable<FieldOption>>();

        foreach (var row in rows.Where(r => r.FieldType is "dropdown" or "multiselect"))
        {
            if (!allOpts.ContainsKey(row.FieldDefinitionId))
                allOpts[row.FieldDefinitionId] = await _optionRepo.GetByFieldIdAsync(row.FieldDefinitionId);
        }

        static string? CurrentValue(FieldPreviewRaw r) => r.FieldType switch
        {
            "number"   => r.ValueNumber?.ToString(),
            "date"     => r.ValueDate?.ToString("yyyy-MM-dd"),
            "boolean"  => r.ValueBoolean?.ToString(),
            _          => r.ValueText,
        };

        static FieldPreviewItemDto ToItem(FieldPreviewRaw r, IEnumerable<FieldOption> opts) => new()
        {
            FieldDefinitionId = r.FieldDefinitionId,
            SectionId         = r.SectionId,
            FieldKey          = r.FieldKey,
            FieldLabel        = r.FieldLabel,
            FieldType         = r.FieldType,
            HelpText          = r.HelpText,
            IsRequired        = r.IsRequired,
            DisplayOrder      = r.DisplayOrder,
            CurrentValue      = CurrentValue(r),
            DisplayFormat     = r.DisplayFormat,
            Options           = opts.Select(o => new FieldOptionDto(o.OptionId, o.FieldDefinitionId, o.OptionKey, o.OptionLabel, o.DisplayOrder, o.IsActive)),
        };

        var sections = rows
            .Where(r => r.SectionId.HasValue)
            .GroupBy(r => (SectionId: r.SectionId!.Value, SectionName: r.SectionName!, SectionDisplayOrder: r.SectionDisplayOrder))
            .OrderBy(g => g.Key.SectionDisplayOrder)
            .Select(g => new SectionPreviewDto
            {
                SectionId    = g.Key.SectionId,
                SectionName  = g.Key.SectionName,
                DisplayOrder = g.Key.SectionDisplayOrder,
                Fields       = g.OrderBy(r => r.DisplayOrder)
                                .Select(r => ToItem(r, allOpts.GetValueOrDefault(r.FieldDefinitionId, []))),
            });

        var unassigned = rows
            .Where(r => !r.SectionId.HasValue)
            .OrderBy(r => r.DisplayOrder)
            .Select(r => ToItem(r, allOpts.GetValueOrDefault(r.FieldDefinitionId, [])));

        return new CustomerFormPreviewDto
        {
            CustomerId       = customerId,
            CustomerName     = $"{customer.FirstName} {customer.LastName}",
            Sections         = sections,
            UnassignedFields = unassigned,
        };
    }

    private static FieldDefinitionDto Map(FieldDefinition entity, FieldSection? section, IEnumerable<FieldOption> opts)
    {
        return new FieldDefinitionDto(
            FieldDefinitionId: entity.FieldDefinitionId,
            OrganizationId: entity.OrganizationId,
            SectionId: entity.FieldSectionId == Guid.Empty ? null : entity.FieldSectionId,
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
            DisplayFormat: entity.DisplayFormat,
            Options: opts.Select(o => new FieldOptionDto(o.OptionId, o.FieldDefinitionId, o.OptionKey, o.OptionLabel, o.DisplayOrder, o.IsActive))
        );
    }


    private static bool IsOptionsField(string fieldType)
        => fieldType is "dropdown" or "multiselect";





}
