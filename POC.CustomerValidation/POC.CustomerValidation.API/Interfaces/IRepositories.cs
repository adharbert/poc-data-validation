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

    /// <summary>Soft-deletes (deactivates) a field section.</summary>
    Task<bool> DeleteAsync(Guid sectionId);

    /// <summary>Activates or deactivates a field section.</summary>
    Task<bool> ChangeStatusAsync(Guid sectionId, bool isActive);

    /// <summary>Bulk-updates DisplayOrder for a set of sections.</summary>
    Task<bool> ReorderAsync(IEnumerable<(Guid SectionId, int DisplayOrder)> updates);
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

    /// <summary>Reorder field definitions.</summary>
    Task<bool> ReorderAsync(IEnumerable<(Guid FieldDefinitionId, int DisplayOrder)> updates);

    /// <summary>Bulk-assigns a section (or null to unassign) and sets display order for a set of fields.</summary>
    Task<bool> BulkAssignSectionAsync(Guid? sectionId, IEnumerable<(Guid FieldId, int DisplayOrder)> assignments);

    /// <summary>Gets all active fields for an org joined with their section and current field values for a customer.</summary>
    Task<IEnumerable<FieldPreviewRaw>> GetPreviewAsync(Guid organizationId, Guid customerId);
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
    Task<IEnumerable<FieldValueHistory>> GetByValueIdAsync(Guid valueId);

    /// <summary>
    /// Get all historical answers by customer Id, paginated.
    /// </summary>
    Task<IEnumerable<FieldValueHistory>> GetByCustomerIdAsync(Guid customerId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Get history for a specific field on a customer.
    /// </summary>
    Task<IEnumerable<FieldValueHistory>> GetByFieldIdAsync(Guid customerId, Guid fieldDefinitionId);
}


public interface ICustomerRepository
{
    /// <summary>Returns a paged list of customers for an organisation.</summary>
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false, int page = 1, int pageSize = 50);

    /// <summary>Returns a single customer by Id.</summary>
    Task<Customer?> GetByIdAsync(Guid customerId);

    /// <summary>Returns a customer matched by email within an organisation (used for deduplication).</summary>
    Task<Customer?> GetByEmailAsync(Guid organisationId, string email);

    /// <summary>Returns a customer matched by OriginalId within an organisation.</summary>
    Task<Customer?> GetByOriginalIdAsync(Guid organisationId, string originalId);

    /// <summary>Creates a new customer. CustomerCode must already be set on the entity.</summary>
    Task<Customer> CreateAsync(Customer customer);

    /// <summary>Updates an existing customer.</summary>
    Task<bool> UpdateAsync(Customer customer);

    /// <summary>Activates or deactivates a customer.</summary>
    Task<bool> ChangeStatusAsync(Guid customerId, bool isActive);
}


public interface IContractRepository
{
    /// <summary>Returns all contracts for an organisation.</summary>
    Task<IEnumerable<Contract>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false);

    /// <summary>Returns a single contract by Id.</summary>
    Task<Contract?> GetByIdAsync(Guid contractId);

    /// <summary>Returns the currently active contract for an organisation, or null.</summary>
    Task<Contract?> GetActiveAsync(Guid organisationId);

    /// <summary>Creates a new contract.</summary>
    Task<Contract> CreateAsync(Contract contract);

    /// <summary>Updates a contract.</summary>
    Task<bool> UpdateAsync(Contract contract);

    /// <summary>Activates or deactivates a contract.</summary>
    Task<bool> ChangeStatusAsync(Guid contractId, bool isActive, string modifiedBy);
}


public interface IMarketingProjectRepository
{
    /// <summary>Returns all marketing projects for an organisation.</summary>
    Task<IEnumerable<MarketingProject>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false);

    /// <summary>Returns a single project by its INT project Id.</summary>
    Task<MarketingProject?> GetByIdAsync(int projectId);

    /// <summary>Creates a new marketing project.</summary>
    Task<MarketingProject> CreateAsync(MarketingProject project);

    /// <summary>Updates a marketing project.</summary>
    Task<bool> UpdateAsync(MarketingProject project);

    /// <summary>Activates or deactivates a project.</summary>
    Task<bool> ChangeStatusAsync(int projectId, bool isActive, string modifiedBy);
}


public interface IImportRepository
{
    /// <summary>Returns a paged list of import batches for an organisation.</summary>
    Task<(IEnumerable<ImportBatch> Items, int TotalCount)> GetBatchesByOrganisationAsync(Guid organisationId, int page = 1, int pageSize = 20);

    /// <summary>Returns a single import batch by Id.</summary>
    Task<ImportBatch?> GetBatchByIdAsync(Guid batchId);

    /// <summary>Creates a new import batch record.</summary>
    Task<ImportBatch> CreateBatchAsync(ImportBatch batch);

    /// <summary>Updates batch status and counters.</summary>
    Task<bool> UpdateBatchAsync(ImportBatch batch);

    /// <summary>Returns all column mappings for a batch.</summary>
    Task<IEnumerable<ImportColumnMapping>> GetMappingsByBatchIdAsync(Guid batchId);

    /// <summary>Replaces all column mappings for a batch in a single transaction.</summary>
    Task SaveMappingsAsync(Guid batchId, IEnumerable<ImportColumnMapping> mappings);

    /// <summary>Writes a row error to ImportErrors.</summary>
    Task AddErrorAsync(ImportError error);

    /// <summary>Returns all errors for a batch.</summary>
    Task<IEnumerable<ImportError>> GetErrorsByBatchIdAsync(Guid batchId);

    /// <summary>Returns saved column mappings for a given org + fingerprint, or empty.</summary>
    Task<IEnumerable<SavedColumnMapping>> GetSavedMappingsAsync(Guid organisationId, string fingerprint);

    /// <summary>Upserts saved column mappings keyed by org + fingerprint + header.</summary>
    Task SaveColumnMappingsAsync(Guid organisationId, string fingerprint, IEnumerable<SavedColumnMapping> mappings);
}


public interface IImportColumnStagingRepository
{
    /// <summary>Returns all staged columns for an organisation, optionally filtered by status.</summary>
    Task<IEnumerable<ImportColumnStaging>> GetByOrganisationIdAsync(Guid organisationId, string? status = null);

    /// <summary>Returns a single staging record by Id.</summary>
    Task<ImportColumnStaging?> GetByIdAsync(Guid stagingId);

    /// <summary>Returns the staging record for a normalised header within an org, or null.</summary>
    Task<ImportColumnStaging?> GetByHeaderAsync(Guid organisationId, string headerNormalized);

    /// <summary>Creates a new staging record.</summary>
    Task<ImportColumnStaging> CreateAsync(ImportColumnStaging staging);

    /// <summary>Updates an existing staging record (resolution or status change).</summary>
    Task<bool> UpdateAsync(ImportColumnStaging staging);

    /// <summary>Increments SeenCount and updates LastSeenAt for an existing unresolved record.</summary>
    Task TouchAsync(Guid stagingId);

    /// <summary>Deletes a staging record.</summary>
    Task<bool> DeleteAsync(Guid stagingId);
}


public interface IDashboardRepository
{
    /// <summary>Returns aggregate stats across all active organisations.</summary>
    Task<DashboardStatsRaw> GetStatsAsync();

    /// <summary>Returns per-organisation customer summary rows.</summary>
    Task<IEnumerable<OrganisationCustomerSummary>> GetOrganisationSummariesAsync();

    /// <summary>Returns active projects whose end date is within warningDays of today.</summary>
    Task<IEnumerable<ExpiringProjectRow>> GetExpiringProjectsAsync(int warningDays);
}

// Raw DB result types used only by DashboardRepository → DashboardService mapping
public class DashboardStatsRaw
{
    public int TotalActiveOrganizations { get; set; }
    public int TotalActiveProjects      { get; set; }
    public int TotalCustomers           { get; set; }
    public int TotalVerifiedCustomers   { get; set; }
}

public class OrganisationCustomerSummary
{
    public Guid     OrganisationId      { get; set; }
    public string   OrganisationName    { get; set; } = default!;
    public int      TotalCustomers      { get; set; }
    public int      VerifiedCustomers   { get; set; }
    public int      ActiveProjects      { get; set; }
}

public class ExpiringProjectRow
{
    public int      ProjectId           { get; set; }
    public string   ProjectName         { get; set; } = default!;
    public Guid     OrganisationId      { get; set; }
    public string   OrganisationName    { get; set; } = default!;
    public DateOnly MarketingEndDate    { get; set; }
    public int      DaysUntilExpiry     { get; set; }
}

public class FieldPreviewRaw
{
    public Guid     FieldDefinitionId   { get; set; }
    public Guid?    SectionId           { get; set; }
    public string?  SectionName         { get; set; }
    public int      SectionDisplayOrder { get; set; }
    public string   FieldKey            { get; set; } = default!;
    public string   FieldLabel          { get; set; } = default!;
    public string   FieldType           { get; set; } = default!;
    public string?  HelpText            { get; set; }
    public bool     IsRequired          { get; set; }
    public int      DisplayOrder        { get; set; }
    public string?  DisplayFormat       { get; set; }
    public string?  ValueText           { get; set; }
    public decimal? ValueNumber         { get; set; }
    public DateOnly? ValueDate          { get; set; }
    public bool?    ValueBoolean        { get; set; }
}