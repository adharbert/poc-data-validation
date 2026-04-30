using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class FieldSectionService(IFieldSectionRepository sectionRepo, IFieldDefinitionRepository fieldRepo) : IFieldSectionService
{
    private readonly IFieldSectionRepository    _sectionRepo = sectionRepo;
    private readonly IFieldDefinitionRepository _fieldRepo   = fieldRepo;

    public async Task<IEnumerable<FieldSectionDto>> GetByOrganizationIdAsync(Guid organizationId)
    {
        var sections = await _sectionRepo.GetByOrganizationIdAsync(organizationId);
        return sections.Select(Map);
    }

    public async Task<FieldSectionDto?> GetByIdAsync(Guid sectionId)
    {
        var section = await _sectionRepo.GetByIdAsync(sectionId);
        return section is null ? null : Map(section);
    }

    public async Task<FieldSectionDto> CreateAsync(Guid organizationId, CreateFieldSectionRequest request)
    {
        var entity = new FieldSection
        {
            SectionId  = Guid.NewGuid(),
            OrganizationId  = organizationId,
            SectionName     = request.SectionName,
            DisplayOrder    = request.DisplayOrder,
            IsActive        = true,
        };
        await _sectionRepo.CreateAsync(entity);
        return Map(entity);
    }

    public async Task<FieldSectionDto> UpdateAsync(Guid sectionId, UpdateFieldSectionRequest request)
    {
        var entity = await _sectionRepo.GetByIdAsync(sectionId)
            ?? throw new KeyNotFoundException("Section not found.");

        entity.SectionName  = request.SectionName;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive     = request.IsActive;

        await _sectionRepo.UpdateAsync(entity);
        return Map(entity);
    }

    public async Task SetStatusAsync(Guid sectionId, bool isActive)
    {
        var ok = await _sectionRepo.ChangeStatusAsync(sectionId, isActive);
        if (!ok) throw new KeyNotFoundException("Section not found.");
    }

    public async Task ReorderAsync(IEnumerable<SectionOrderItem> items)
    {
        var updates = items.Select(i => (i.SectionId, i.DisplayOrder));
        await _sectionRepo.ReorderAsync(updates);
    }

    public async Task AssignFieldsAsync(Guid sectionId, AssignFieldsToSectionRequest request)
    {
        var section = await _sectionRepo.GetByIdAsync(sectionId)
            ?? throw new KeyNotFoundException("Section not found.");

        var newFieldIds = request.Fields.Select(f => f.FieldDefinitionId).ToHashSet();

        // Clear fields that were previously in this section but are no longer selected
        var currentFields = await _fieldRepo.GetBySectionIdAsync(section.SectionId);
        var toUnassign = currentFields
            .Where(f => !newFieldIds.Contains(f.FieldDefinitionId))
            .Select(f => (f.FieldDefinitionId, 0))
            .ToList();

        if (toUnassign.Count > 0)
            await _fieldRepo.BulkAssignSectionAsync(null, toUnassign);

        // Assign the selected fields to this section
        if (newFieldIds.Count > 0)
        {
            var assignments = request.Fields.Select(f => (f.FieldDefinitionId, f.DisplayOrder));
            await _fieldRepo.BulkAssignSectionAsync(section.SectionId, assignments);
        }
    }

    private static FieldSectionDto Map(FieldSection s) =>
        new(s.SectionId, s.OrganizationId, s.SectionName, s.DisplayOrder, s.IsActive);
}
