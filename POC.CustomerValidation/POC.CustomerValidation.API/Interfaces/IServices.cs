using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Interfaces;

public interface IOrganizationServices
{
    /// <summary>
    /// Get all organizations, with an option to include inactive ones.
    /// </summary>
    /// <param name="includeInactive">boolean</param>
    /// <returns>IEnumerable of OrganizationDto</returns>
    Task<IEnumerable<OrganizationDto>> GetAllAsync(bool includeInactive = false);

    /// <summary>
    /// Get organization details by its unique identifier (OrganizationId).
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <returns>OrganizationDto</returns>
    Task<OrganizationDto?> GetByIdAsync(Guid organizationId);

    /// <summary>
    /// Creates a new organization asynchronously based on the specified request.
    /// </summary>
    /// <param name="request">CreateOrganizationRequest</param>
    /// <returns>OrganizationDto</returns>
    Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request);

    /// <summary>
    /// Update an existing organization's details asynchronously based on the specified organizationId and update request.
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <param name="request">UpdateOrganizationRequest</param>
    /// <returns></returns>
    Task<OrganizationDto> UpdateAsync(Guid organizationId, UpdateOrganizationRequest request);

    /// <summary>
    /// Change the active status of an organization asynchronously based on the specified organizationId and desired active status.
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <param name="isActive">Boolean</param>
    Task ChangeStatus(Guid organizationId, bool isActive);

}

public interface IFieldDefinitionService
{
    /// <summary>
    /// Get all field definitions for specified organization, with an option to include inactive ones.
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <param name="includeInactive">Boolean</param>
    /// <returns>IEnumerable of FieldDefinitionDto</returns>
    Task<IEnumerable<FieldDefinitionDto>> GetByOrganizationIdAsync(Guid organizationId, bool includeInactive = false);

    /// <summary>
    /// Get field definition details by its unique identifier (FieldDefinitionId).
    /// </summary>
    /// <param name="fieldDefinitionId">Guid</param>
    /// <returns>FieldDefinitionDto</returns>
    Task<FieldDefinitionDto?> GetByIdAsync(Guid fieldDefinitionId);

    /// <summary>
    /// Create new Field Definition asynchronously based on the specified request, and return the created FieldDefinitionDto.
    /// </summary>
    /// <param name="request">CreateFieldDefinitionRequest</param>
    /// <returns>FieldDefinitionDto</returns>
    Task<FieldDefinitionDto> CreateAsync(CreateFieldDefinitionRequest request);

    /// <summary>
    /// Update an existing Field Definition's details asynchronously based on the specified fieldDefinitionId and update request, and return the updated FieldDefinitionDto.
    /// </summary>
    /// <param name="request">UpdateFieldDefinitionRequest</param>
    /// <returns>FieldDefinitionDto</returns>
    Task<FieldDefinitionDto> UpdateAsync(UpdateFieldDefinitionRequest request);

    /// <summary>
    /// Set status (active/inactive) of a Field Definition asynchronously based on the specified fieldDefinitionId and desired active status.
    /// </summary>
    /// <param name="fieldDefinitionId">Guid</param>
    /// <param name="isActive">Boolean</param>
    Task SetStatusAsync(Guid fieldDefinitionId, bool isActive);
    
    /// <summary>
    /// Reorder field definitions asynchronously based on the specified organizationId and updates.
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <param name="updates">IEnumerable of tuples containing fieldDefinitionId and displayOrder</param>
    Task ReorderAsync(Guid organizationId, IEnumerable<(Guid fieldDefinitionId, int displayOrder)> updates);
}

public interface IFieldOptionService
{
    /// <summary>
    /// Get all field options for specified field definition, with an option to include inactive ones.
    /// </summary>
    /// <param name="fieldDefinitionId">Guid</param>
    /// <returns>IEnumerable of FieldOptionDto</returns>
    Task<IEnumerable<FieldOptionDto>> GetByFieldDefinitionIdAsync(Guid fieldDefinitionId);
    
    /// <summary>
    /// Create a new field option asynchronously for the specified field definition.
    /// </summary>
    /// <param name="fieldDefinitionId">Guid</param>
    /// <param name="request">CreateFieldOptionRequest</param>
    /// <returns>FieldOptionDto</returns>
    Task<FieldOptionDto> CreateAsync(Guid fieldDefinitionId, CreateFieldOptionRequest request);

    /// <summary>
    /// Update an existing field option asynchronously based on the specified optionId and update request, and return the updated FieldOptionDto.
    /// </summary>
    /// <param name="optionId">Guid</param>
    /// <param name="request">UpdateFieldOptionRequest</param>
    /// <returns>FieldOptionDto</returns>
    Task<FieldOptionDto> UpdateAsync(Guid optionId, UpdateFieldOptionRequest request);

    /// <summary>
    /// Bulk upsert field options asynchronously for the specified field definition.
    /// </summary>
    /// <param name="fieldDefinitionId">Guid</param>
    /// <param name="request">BulkUpsertFieldOptionsRequest</param>
    Task BulkUpsertAsync(Guid fieldDefinitionId, BulkUpsertFieldOptionsRequest request);

    /// <summary>
    /// Delete a field option asynchronously based on the specified optionId.
    /// </summary>
    /// <param name="optionId">Guid</param>
    Task DeleteAsync(Guid optionId);
}

public interface IFieldValueService
{
    /// <summary>
    /// Asynchronously retrieves all field values associated with the specified customer identifier.
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <returns>IEnumerable of FieldValueDto</returns>
    Task<IEnumerable<FieldValueDto>> GetByCustomerIdAsync(Guid customerId);

    /// <summary>
    /// Get history for customer's answers and data changes.
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <param name="page">Int</param>
    /// <param name="pageSize">Int</param>
    /// <returns>IEnumerable of FieldValueHistoryDto</returns>
    Task<IEnumerable<FieldValueHistoryDto>> GetHistoryByCustomerAsync(Guid customerId, int page, int pageSize);

    /// <summary>
    /// Get history for customer on specific field definition.
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <param name="fieldDefinitionId">Guid</param>
    /// <returns>IEnumerable of FieldValueHistoryDto</returns>
    Task<IEnumerable<FieldValueHistoryDto>> GetHistoryByFieldAsync(Guid customerId, Guid fieldDefinitionId);
}