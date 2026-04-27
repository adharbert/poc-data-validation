using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class ImportStagingService(
    IImportColumnStagingRepository repo,
    IFieldDefinitionRepository fieldRepo) : IImportStagingService
{
    private readonly IImportColumnStagingRepository _repo      = repo;
    private readonly IFieldDefinitionRepository     _fieldRepo = fieldRepo;

    public async Task<IEnumerable<ImportColumnStagingDto>> GetByOrganisationIdAsync(Guid organisationId, string? status = null)
    {
        var rows = await _repo.GetByOrganisationIdAsync(organisationId, status);
        return await MapManyAsync(rows);
    }

    public async Task<ImportColumnStagingDto?> GetByIdAsync(Guid stagingId)
    {
        var row = await _repo.GetByIdAsync(stagingId);
        if (row is null) return null;
        return await MapAsync(row);
    }

    public async Task<ImportColumnStagingDto> ResolveAsync(Guid stagingId, ResolveColumnStagingRequest request)
    {
        var staging = await _repo.GetByIdAsync(stagingId)
            ?? throw new KeyNotFoundException($"Staging record {stagingId} not found.");

        staging.Status              = request.Status;
        staging.MappingType         = request.MappingType;
        staging.CustomerFieldName   = request.CustomerFieldName;
        staging.FieldDefinitionId   = request.FieldDefinitionId;
        staging.Notes               = request.Notes?.Trim();
        staging.ResolvedAt          = DateTime.UtcNow;
        staging.ResolvedBy          = request.ResolvedBy;
        staging.LastSeenAt          = DateTime.UtcNow;

        await _repo.UpdateAsync(staging);
        return await MapAsync(staging);
    }

    public async Task DeleteAsync(Guid stagingId)
    {
        var ok = await _repo.DeleteAsync(stagingId);
        if (!ok) throw new KeyNotFoundException($"Staging record {stagingId} not found.");
    }

    private async Task<IEnumerable<ImportColumnStagingDto>> MapManyAsync(IEnumerable<ImportColumnStaging> rows)
    {
        var result = new List<ImportColumnStagingDto>();
        foreach (var row in rows)
            result.Add(await MapAsync(row));
        return result;
    }

    private async Task<ImportColumnStagingDto> MapAsync(ImportColumnStaging s)
    {
        string? fieldLabel = null;
        if (s.FieldDefinitionId.HasValue)
        {
            var fd = await _fieldRepo.GetByIdAsync(s.FieldDefinitionId.Value);
            fieldLabel = fd?.FieldLabel;
        }

        return new ImportColumnStagingDto
        {
            StagingId           = s.StagingId,
            OrganizationId      = s.OrganizationId,
            CsvHeader           = s.CsvHeader,
            Status              = s.Status,
            MappingType         = s.MappingType,
            CustomerFieldName   = s.CustomerFieldName,
            FieldDefinitionId   = s.FieldDefinitionId,
            FieldLabel          = fieldLabel,
            SeenCount           = s.SeenCount,
            FirstSeenAt         = s.FirstSeenAt,
            LastSeenAt          = s.LastSeenAt,
            ResolvedAt          = s.ResolvedAt,
            ResolvedBy          = s.ResolvedBy,
            Notes               = s.Notes,
        };
    }
}
