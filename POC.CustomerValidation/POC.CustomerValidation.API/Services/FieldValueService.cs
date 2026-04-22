using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class FieldValueService(IFieldValueRepository valueRepo, IFieldValueHistoryRepository historyRepo, IFieldDefinitionRepository fieldRepo) : IFieldValueService
{
    private readonly IFieldValueRepository _valueRepo = valueRepo;
    private readonly IFieldValueHistoryRepository _historyRepo = historyRepo;
    private readonly IFieldDefinitionRepository _fieldRepo = fieldRepo;



    public async Task<IEnumerable<FieldValueDto>> GetByCustomerIdAsync(Guid customerId)
    {
        var values = await _valueRepo.GetByCustomerIdAsync(customerId);
        var results = new List<FieldValueDto>();

        foreach (var v in values)
        {
            var field = await _fieldRepo.GetByIdAsync(v.FieldDefinitionId);
            results.Add(MapValue(v, field));
        }

        return results;
    }

    public async Task<IEnumerable<FieldValueHistoryDto>> GetHistoryByCustomerAsync(Guid customerId, int page, int pageSize)
    {
        var history = await _historyRepo.GetByCustomerIdAsync(customerId, page, pageSize);
        var result = new List<FieldValueHistoryDto>();

        foreach (var h in history)
        {
            var field = await _fieldRepo.GetByIdAsync(h.FieldDefinitionId);
            result.Add(MapHistory(h, field));
        }

        return result;
    }

    public async Task<IEnumerable<FieldValueHistoryDto>> GetHistoryByFieldAsync(Guid customerId, Guid fieldDefinitionId)
    {
        var history = await _historyRepo.GetByFieldIdAsync(customerId, fieldDefinitionId);
        var field = await _fieldRepo.GetByIdAsync(fieldDefinitionId);
        return history.Select(h => MapHistory(h, field));
    }







    private static FieldValueDto MapValue(FieldValue v, FieldDefinition? field)
    {
        return new FieldValueDto(
            v.FieldValueId,
            v.CustomerId,
            v.FieldDefinitionId,
            field?.FieldLabel ?? string.Empty,
            field?.FieldType ?? string.Empty,
            v.ValueText,
            v.ValueNumber,
            v.ValueDate,
            v.ValueDatetime,
            v.ValueBoolean,
            CoalesceDisplayValue(v),  // consoledate the values into this field.            
            v.ConfirmedAt,
            v.ConfirmedBy,
            v.FlaggedAt,
            v.FlagNote,        
            v.CreatedDt,       
            v.ModifiedDt       
        );
    }


    private static FieldValueHistoryDto MapHistory(FieldValueHistory h, FieldDefinition? field)
    {
        return new FieldValueHistoryDto(
            h.HistoryId,
            h.FieldValueId,
            h.FieldDefinitionId,
            h.CustomerId,
            field?.FieldLabel ?? string.Empty,
            CoalesceHistoryValue(h),        // OldValue display here
            h.ChangedBy,
            h.ChangeAt,
            h.ChangeReason
        );
    }





    private static string? CoalesceDisplayValue(FieldValue v) =>
        v.ValueText
            ?? v.ValueNumber?.ToString()
            ?? v.ValueDate?.ToString("yyyy-MM-dd")
            ?? v.ValueDatetime?.ToString("yyyy-MM-dd HH:mm:ss")
            ?? (v.ValueBoolean.HasValue ? (v.ValueBoolean.Value ? "Yes" : "No") : null
    );



    private static string? CoalesceHistoryValue(FieldValueHistory v) =>
        v.ValueText
            ?? v.ValueNumber?.ToString()
            ?? v.ValueDate?.ToString("yyyy-MM-dd")
            ?? v.ValueDateTime?.ToString("yyyy-MM-dd HH:mm:ss")
            ?? (v.ValueBoolean.HasValue ? (v.ValueBoolean.Value ? "Yes" : "No") : null
    );


}
