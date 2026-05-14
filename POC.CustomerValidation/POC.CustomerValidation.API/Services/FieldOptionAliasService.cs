using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class FieldOptionAliasService(IFieldOptionAliasRepository repo) : IFieldOptionAliasService
{
    private readonly IFieldOptionAliasRepository _repo = repo;

    public async Task<IEnumerable<FieldOptionAliasDto>> GetByOrganizationAsync(Guid organizationId)
    {
        var aliases = await _repo.GetByOrganizationAsync(organizationId);
        return aliases.Select(Map);
    }

    public async Task<IEnumerable<FieldOptionAliasDto>> BulkSaveAsync(Guid organizationId, BulkSaveAliasesRequest request)
    {
        var entities = request.Aliases.Select(a => new FieldOptionAlias
        {
            OrganizationId    = organizationId,
            FieldDefinitionId = a.FieldDefinitionId,
            AliasValue        = a.AliasValue.Trim(),
            CanonicalValue    = a.CanonicalValue.Trim(),
            CreatedDt         = DateTime.UtcNow,
            ModifiedDt        = DateTime.UtcNow,
        }).ToList();

        await _repo.BulkUpsertAsync(entities);
        var saved = await _repo.GetByOrganizationAndFieldsAsync(
            organizationId,
            entities.Select(e => e.FieldDefinitionId).Distinct());
        return saved.Select(Map);
    }

    public Task DeleteAsync(Guid id) => _repo.DeleteAsync(id);

    private static FieldOptionAliasDto Map(FieldOptionAlias a) => new()
    {
        Id                = a.Id,
        OrganizationId    = a.OrganizationId,
        FieldDefinitionId = a.FieldDefinitionId,
        AliasValue        = a.AliasValue,
        CanonicalValue    = a.CanonicalValue,
        CreatedDt         = a.CreatedDt,
    };
}
