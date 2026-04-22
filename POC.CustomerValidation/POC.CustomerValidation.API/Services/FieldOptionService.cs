using POC.CustomerValidation.API.Extensions;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class FieldOptionService(IFieldOptionRepository repo) : IFieldOptionService
{
    private readonly IFieldOptionRepository _repo = repo;




    public async Task<IEnumerable<FieldOptionDto>> GetByFieldDefinitionIdAsync(Guid fieldDefinitionId)
    {
        var opts = await _repo.GetByFieldIdAsync(fieldDefinitionId);
        return opts.Select(Map);
    }

    public async Task<FieldOptionDto> CreateAsync(Guid fieldDefinitionId, CreateFieldOptionRequest request)
    {
        var entity = new FieldOption
        {
            FieldDefinitionId   = fieldDefinitionId,
            OptionKey           = request.OptionKey,
            OptionLabel         = request.OptionLabel,
            DisplayOrder        = request.DisplayOrder,
            IsActive            = true
        };

        var entityId = await _repo.CreateAsync(entity);
        entity.OptionId = entityId;
        return Map(entity);
    }

    public async Task<FieldOptionDto> UpdateAsync(Guid optionId, UpdateFieldOptionRequest request)
    {
        var entity = await _repo.GetByIdAsync(optionId) ?? throw new KeyNotFoundException($"Option {optionId} not found.");

        entity.UpdateOptionFromRequest(request);
        await _repo.UpdateAsync(entity);

        return Map(entity);
    }

    public async Task BulkUpsertAsync(Guid fieldDefinitionId, BulkUpsertFieldOptionsRequest request)
    {
        var entities = request.Options.Select((o, i) => new FieldOption
        {
            FieldDefinitionId   = fieldDefinitionId,
            OptionKey           = o.OptionKey.Trim().ToLowerInvariant(),
            OptionLabel         = o.OptionLabel.Trim(),
            DisplayOrder        = o.DisplayOrder == 0 ? i + 1 : o.DisplayOrder,
            IsActive            = true
        });

        await _repo.BulkUpsertAsync(fieldDefinitionId, entities);
    }

    public async Task DeleteAsync(Guid optionId)
    {
        var ok = await _repo.DeleteAsync(optionId);
        if (!ok) throw new KeyNotFoundException($"Option {optionId} not found.");
    }



    private static FieldOptionDto Map(FieldOption o)
    {
        return new FieldOptionDto(
            o.OptionId,
            o.FieldDefinitionId,
            o.OptionKey,
            o.OptionLabel,
            o.DisplayOrder,
            o.IsActive
         );
    }


}
