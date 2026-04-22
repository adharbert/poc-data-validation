using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Interfaces;

public interface IOrganizationRepository
{
    /// <summary>
    /// Query to return all Organizations.  By default, it will return only active organizations. If includeInactive is set to true, it will return all organizations including inactive ones.
    /// </summary>
    /// <param name="includeInactive"></param>
    /// <returns>List of Organizations</returns>
    Task<IEnumerable<Organization>> GetAllAsync(bool includeInactive = false);

    /// <summary>
    /// Get organization by organizationId.
    /// </summary>
    /// <param name="OrganizationId">Guid</param>
    /// <returns>Organization</returns>
    Task<Organization?> GetByIdAsync(Guid OrganizationId);

    /// <summary>
    /// Get organization by organizationCode.
    /// </summary>
    /// <param name="organizationCode">string</param>
    /// <returns>Organization</returns>
    Task<Organization?> GetByOrganizationCodeAsync(string organizationCode);

    /// <summary>
    /// Create new Organization.  It will return the organizationId of the newly created organization.
    /// </summary>
    /// <param name="organization">Organization</param>
    /// <returns>Organization</returns>
    Task<Organization> CreateAsync(Organization organization);

    /// <summary>
    /// Updates existing organization.  It will return true if the update is successful, otherwise false.  It will return false if the organization does not exist or if there is an error during the update process.
    /// </summary>
    /// <param name="organization">Organization</param>
    /// <returns>Boolean</returns>
    Task<bool> UpdateAsync(Organization organization);

    /// <summary>
    /// Sets organization as inactive.  It will return true if the deactivation is successful, otherwise false.  It will return false if the organization does not exist or if there is an error during the deactivation process.
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <returns>Boolean</returns>
    Task<bool> ChangeStatusAsync(Guid organizationId, bool isActive = true, string modifiedBy = "System");

    /// <summary>
    /// Checks if organization exists by organizationId.
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <returns>Boolean</returns>
    Task<bool> ExistsAsync(Guid organizationId);
}


public interface IFieldSectionRepository
{
    /// <summary>
    /// Gets all Field Sections by Organization.  
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <returns>IEnumberable List FieldSections</returns>
    Task<IEnumerable<FieldSection>> GetByOrganizationIdAsync(Guid organizationId);

    /// <summary>
    /// Get Field Sections by Field Section Id.
    /// </summary>
    /// <param name="fieldSectionId">Guid</param>
    /// <returns>FieldSection</returns>
    Task<FieldSection?> GetByIdAsync(Guid fieldSectionId);

    /// <summary>
    /// Create new field section. If successful, it will return the fieldSectionId of the newly created field section.
    /// </summary>
    /// <param name="fieldSection"></param>
    /// <returns>Guid</returns>
    Task<Guid> CreateAsync(FieldSection fieldSection);

    /// <summary>
    /// Update existing Field Sections.  It will return true if the update is successful, otherwise false.  It will return false if the field section does not exist or if there is an error during the update process.
    /// </summary>
    /// <param name="fieldSection">Guid</param>
    /// <returns>Boolean</returns>
    Task<bool> UpdateAsync(FieldSection fieldSection);

    /// <summary>
    /// Delete existing Field Section.  It will return true if the deletion is successful, otherwise false.  It will return false if the field section does not exist or if there is an error during the deletion process.
    /// </summary>
    /// <param name="sectionId">Guid</param>
    /// <returns>Boolean</returns>
    Task<bool> DeleteAsync(Guid sectionId);

}


public interface IFieldDefinitionRepository
{
    /// <summary>
    /// Get field definitions by organizationId.  By default, it will return only active field definitions. If includeInactive is set to true, it will return all field definitions including inactive ones.
    /// </summary>
    /// <param name="organizationId">Guid</param>
    /// <param name="includeInactive">Boolean</param>
    /// <returns>IEnumerable of FieldDefinition</returns>
    Task<IEnumerable<FieldDefinition>> GetByOrganizationIdAsync(Guid organizationId, bool includeInactive = false);

    /// <summary>
    /// Get by section Ids.
    /// </summary>
    /// <param name="sectionId">Guid</param>
    /// <returns>IEnumerable of FieldDefinition</returns>
    Task<IEnumerable<FieldDefinition>> GetBySectionIdAsync(Guid sectionId);

    /// <summary>
    /// Get by field definition Id.
    /// </summary>
    /// <param name="FieldDefinitionId">Guid</param>
    /// <returns>FieldDefinition</returns>
    Task<FieldDefinition?> GetByIdAsync(Guid FieldDefinitionId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="organizationId"></param>
    /// <param name="fieldKey"></param>
    /// <returns></returns>
    Task<FieldDefinition?> GetByKeyAsync(Guid organizationId, string fieldKey);

    /// <summary>
    /// Create new field definition. If successful, it will return the fieldDefinitionId of the newly created field definition.
    /// </summary>
    /// <param name="fieldDefinition">FieldDefinition</param>
    /// <returns>Guid</returns>
    Task<Guid> CreateAsync(FieldDefinition fieldDefinition);

    /// <summary>
    /// Update FieldDefinition.  Returns true if updated.
    /// </summary>
    /// <param name="fieldDefinition">FieldDefinition</param>
    /// <returns>Boolean</returns>
    Task<bool> UpdateAsync(FieldDefinition fieldDefinition);

    /// <summary>
    /// Change the status of a field definition. Returns true if the status change is successful.
    /// </summary>
    /// <param name="FieldDefinitionId">`Guid</param>
    /// <param name="IsActive">Boolean</param>
    /// <returns>Boolean</returns>
    Task<bool> ChangeStatusAsync(Guid FieldDefinitionId, bool IsActive);

    /// <summary>
    /// Reorder field definitions. Returns true if the reordering is successful.
    /// </summary>
    /// <param name="updates">IEnumerable list of Guid and Int</param>
    /// <returns>Boolean</returns>
    Task<bool> ReorderAsync(IEnumerable<(Guid FieldDefinitionId, int DisplayOrder)> updates);
}


public interface IFieldOptionRepository
{
    /// <summary>
    /// Get all field options by field definition Id. By default it will return only active, can specifiy inactive as well.
    /// </summary>
    /// <param name="FieldDefinitionId">Guid</param>
    /// <param name="includeInactive">Boolean</param>
    /// <returns>IEnumberable of FieldOptions</returns>
    Task<IEnumerable<FieldOption>> GetByFieldIdAsync(Guid FieldDefinitionId, bool includeInactive = false);

    /// <summary>
    ///  Get Field Option by option Id.
    /// </summary>
    /// <param name="optionId">Guid</param>
    /// <returns>FieldOption</returns>
    Task<FieldOption?> GetByIdAsync(Guid optionId);

    /// <summary>
    /// Create Field Option.  Returns Id.
    /// </summary>
    /// <param name="option">FieldOption</param>
    /// <returns>Guid</returns>
    Task<Guid> CreateAsync(FieldOption option);

    /// <summary>
    /// Update field option.  Returns true if updated successfully.
    /// </summary>
    /// <param name="option">FieldOption</param>
    /// <returns>Boolean</returns>
    Task<bool> UpdateAsync(FieldOption option);

    /// <summary>
    /// Delete field option.  Returns true if deleted successfully.
    /// </summary>
    /// <param name="optionId">Guid</param>
    /// <returns>Boolean</returns>
    Task<bool> DeleteAsync(Guid optionId);

    /// <summary>
    /// Bulk upsert for field (FieldDefinition) options. Replaces all options for a field in a single transaction. 
    /// </summary>
    /// <param name="FieldDefinitionId">Guid</param>
    /// <param name="options">IEnumerable of FieldOption</param>
    Task BulkUpsertAsync(Guid FieldDefinitionId, IEnumerable<FieldOption> options);
}


public interface IFieldValueRepository
{
    /// <summary>
    /// Get all field answers by customer Id. 
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <returns>IEnumerable of FieldValue</returns>
    Task<IEnumerable<FieldValue>> GetByCustomerIdAsync(Guid customerId);

    /// <summary>
    /// Get Field value of specified field description by customer Id.
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <param name="FieldDescriptionId">Guid</param>
    /// <returns></returns>
    Task<FieldValue?> GetByCustomerAndFieldAsync(Guid customerId, Guid FieldDescriptionId);

    /// <summary>
    /// Insert / Update call for FieldValue.
    /// </summary>
    /// <param name="fieldValue">FieldValue</param>
    /// <returns></returns>
    Task<Guid> UpsertAsync(FieldValue fieldValue);

    /// <summary>
    /// Adds confirmation if no fields were updated but are validated.  If internal user marks it as confirmed, it will show who confirmed it.
    /// </summary>
    /// <param name="valueId">Guid</param>
    /// <param name="confirmedBy">string</param>
    /// <returns>Boolean</returns>
    Task<bool> ConfirmAsync(Guid valueId, string confirmedBy);

    /// <summary>
    /// Flagging fields and adding a note if needed.
    /// </summary>
    /// <param name="valueId">Guid</param>
    /// <param name="FlagNote">string</param>
    /// <returns>Boolean</returns>
    Task<bool>FlagAsync(Guid valueId, string FlagNote);
}



public interface IFieldValueHistoryRepository
{
    /// <summary>
    /// Get all history of specified field answers.
    /// </summary>
    /// <param name="valueId">Guid</param>
    /// <returns>IEnumerable of FieldValueHistory</returns>
    Task<IEnumerable<FieldValueHistory>> GetByValueIdAsync(Guid valueId);

    /// <summary>
    /// Get all historical answers by customer Id.  This is for audit use, so it will return all historical answers of all fields for a customer.  It will be paginated to avoid returning too much data at once.
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <param name="page">Int</param>
    /// <param name="pageSize">Int</param>
    /// <returns>IEnumerable of FieldValueHistory</returns>
    Task<IEnumerable<FieldValueHistory>> GetByCustomerIdAsync(Guid customerId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Get 
    /// </summary>
    /// <param name="customerId"></param>
    /// <param name="fieldDefinitionId"></param>
    /// <returns></returns>
    Task<IEnumerable<FieldValueHistory>> GetByFieldIdAsync(Guid customerId, Guid fieldDefinitionId);
}