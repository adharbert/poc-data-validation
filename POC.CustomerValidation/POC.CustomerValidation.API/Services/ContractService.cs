using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Services;

public class ContractService(IContractRepository repo, IOrganizationRepository orgRepo) : IContractService
{
    private readonly IContractRepository    _repo    = repo;
    private readonly IOrganizationRepository _orgRepo = orgRepo;

    public async Task<IEnumerable<ContractDto>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false)
    {
        var contracts = await _repo.GetByOrganisationIdAsync(organisationId, includeInactive);
        return contracts.Select(Map);
    }

    public async Task<ContractDto?> GetByIdAsync(Guid contractId)
    {
        var contract = await _repo.GetByIdAsync(contractId);
        return contract is null ? null : Map(contract);
    }

    public async Task<ContractDto> CreateAsync(Guid organisationId, CreateContractRequest request)
    {
        var orgExists = await _orgRepo.ExistsAsync(organisationId);
        if (!orgExists)
            throw new KeyNotFoundException($"Organisation {organisationId} not found.");

        var active = await _repo.GetActiveAsync(organisationId);
        if (active is not null)
            throw new InvalidOperationException($"Organisation {organisationId} already has an active contract (Id: {active.ContractId}). Deactivate it first.");

        var contract = new Contract
        {
            OrganizationId  = organisationId,
            ContractName    = request.ContractName.Trim(),
            ContractNumber  = request.ContractNumber?.Trim(),
            StartDate       = request.StartDate,
            EndDate         = request.EndDate,
            IsActive        = true,
            Notes           = request.Notes?.Trim(),
            CreatedBy       = request.CreatedBy,
        };

        var created = await _repo.CreateAsync(contract);
        return Map(created);
    }

    public async Task<ContractDto> UpdateAsync(Guid contractId, UpdateContractRequest request)
    {
        var contract = await _repo.GetByIdAsync(contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        contract.ContractName   = request.ContractName.Trim();
        contract.ContractNumber = request.ContractNumber?.Trim();
        contract.StartDate      = request.StartDate;
        contract.EndDate        = request.EndDate;
        contract.Notes          = request.Notes?.Trim();
        contract.ModifiedBy     = request.ModifiedBy;

        await _repo.UpdateAsync(contract);
        return Map(contract);
    }

    public async Task ChangeStatusAsync(Guid contractId, bool isActive, string modifiedBy)
    {
        if (isActive)
        {
            var contract = await _repo.GetByIdAsync(contractId)
                ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

            var active = await _repo.GetActiveAsync(contract.OrganizationId);
            if (active is not null && active.ContractId != contractId)
                throw new InvalidOperationException($"Organisation already has an active contract (Id: {active.ContractId}).");
        }

        var ok = await _repo.ChangeStatusAsync(contractId, isActive, modifiedBy);
        if (!ok) throw new KeyNotFoundException($"Contract {contractId} not found.");
    }

    private static ContractDto Map(Contract c) => new()
    {
        ContractId      = c.ContractId,
        OrganizationId  = c.OrganizationId,
        ContractName    = c.ContractName,
        ContractNumber  = c.ContractNumber,
        StartDate       = c.StartDate,
        EndDate         = c.EndDate,
        IsActive        = c.IsActive,
        Notes           = c.Notes,
        CreatedDt       = c.CreatedDt,
        CreatedBy       = c.CreatedBy,
        ModifiedDt      = c.ModifiedDt,
        ModifiedBy      = c.ModifiedBy,
    };
}
