using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Interfaces;

public interface IOrganizationServices
{
    /// <summary>
    /// Get all organizations, with an option to include inactive ones.
    /// </summary>
    /// <param name="includeInactive">boolean</param>
    /// <returns>IEnumerable of OrganizationDto</returns>
    Task<IEnumerable<OrganizationDto>> GetAllAsync(bool includeInactive = false, string? search = null);

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

    /// <summary>
    /// Forces re-provisioning of the isolated database regardless of current status.
    /// Safe to call on already-provisioned orgs — EnsureDatabase, DbUp, and seed are all idempotent.
    /// </summary>
    Task ReprovisionAsync(Guid organizationId);

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
    
    /// <summary>Reorder field definitions.</summary>
    Task ReorderAsync(Guid organizationId, IEnumerable<(Guid fieldDefinitionId, int displayOrder)> updates);

    /// <summary>Returns all active fields for the org grouped by section with the customer's current values.</summary>
    Task<CustomerFormPreviewDto> GetFormPreviewAsync(Guid organizationId, Guid customerId);
}

public interface IFieldSectionService
{
    Task<IEnumerable<FieldSectionDto>> GetByOrganizationIdAsync(Guid organizationId);
    Task<FieldSectionDto?> GetByIdAsync(Guid sectionId);
    Task<FieldSectionDto> CreateAsync(Guid organizationId, CreateFieldSectionRequest request);
    Task<FieldSectionDto> UpdateAsync(Guid sectionId, UpdateFieldSectionRequest request);
    Task SetStatusAsync(Guid sectionId, bool isActive);
    Task ReorderAsync(IEnumerable<SectionOrderItem> items);
    Task AssignFieldsAsync(Guid sectionId, AssignFieldsToSectionRequest request);
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
    /// <summary>Retrieves all field values for a customer.</summary>
    Task<IEnumerable<FieldValueDto>> GetByCustomerIdAsync(Guid customerId);

    /// <summary>Get paginated change history for all fields of a customer.</summary>
    Task<IEnumerable<FieldValueHistoryDto>> GetHistoryByCustomerAsync(Guid customerId, int page, int pageSize);

    /// <summary>Get change history for a specific field on a customer.</summary>
    Task<IEnumerable<FieldValueHistoryDto>> GetHistoryByFieldAsync(Guid customerId, Guid fieldDefinitionId);
}


public interface ICustomerService
{
    /// <summary>Returns a paged list of customers for an organisation.</summary>
    Task<PagedResult<CustomerDto>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false, int page = 1, int pageSize = 50);

    /// <summary>Returns a single customer by Id.</summary>
    Task<CustomerDto?> GetByIdAsync(Guid customerId);

    /// <summary>Creates a single customer manually. CustomerCode is generated by the service.</summary>
    Task<CustomerDto> CreateAsync(Guid organisationId, CreateCustomerRequest request);

    /// <summary>Updates a customer's details.</summary>
    Task<CustomerDto> UpdateAsync(Guid customerId, UpdateCustomerRequest request);

    /// <summary>Activates or deactivates a customer.</summary>
    Task ChangeStatusAsync(Guid customerId, bool isActive);

    /// <summary>Returns all email records for a customer.</summary>
    Task<IEnumerable<CustomerEmailDto>> GetEmailsByCustomerIdAsync(Guid customerId);

    /// <summary>Returns all phone records for a customer.</summary>
    Task<IEnumerable<CustomerPhoneDto>> GetPhonesByCustomerIdAsync(Guid customerId);
}


public interface IContractService
{
    /// <summary>Returns all contracts for an organisation.</summary>
    Task<IEnumerable<ContractDto>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false);

    /// <summary>Returns a single contract.</summary>
    Task<ContractDto?> GetByIdAsync(Guid contractId);

    /// <summary>Creates a new contract. Throws if one is already active for the org.</summary>
    Task<ContractDto> CreateAsync(Guid organisationId, CreateContractRequest request);

    /// <summary>Updates a contract's details.</summary>
    Task<ContractDto> UpdateAsync(Guid contractId, UpdateContractRequest request);

    /// <summary>Activates or deactivates a contract.</summary>
    Task ChangeStatusAsync(Guid contractId, bool isActive, string modifiedBy);
}


public interface IMarketingProjectService
{
    /// <summary>Returns all marketing projects for an organisation.</summary>
    Task<IEnumerable<MarketingProjectDto>> GetByOrganisationIdAsync(Guid organisationId, bool includeInactive = false);

    /// <summary>Returns a single project.</summary>
    Task<MarketingProjectDto?> GetByIdAsync(int projectId);

    /// <summary>Creates a new marketing project.</summary>
    Task<MarketingProjectDto> CreateAsync(Guid organisationId, CreateMarketingProjectRequest request);

    /// <summary>Updates a project.</summary>
    Task<MarketingProjectDto> UpdateAsync(int projectId, UpdateMarketingProjectRequest request);

    /// <summary>Activates or deactivates a project.</summary>
    Task ChangeStatusAsync(int projectId, bool isActive, string modifiedBy);
}


public interface IImportService
{
    /// <summary>Uploads a file, auto-matches headers, creates the ImportBatch.</summary>
    Task<UploadImportResponseDto> UploadAsync(Guid organisationId, IFormFile file, string uploadedBy, string duplicateStrategy = "skip");

    /// <summary>Returns saved mappings for a given fingerprint, if any.</summary>
    Task<IEnumerable<ColumnMatchResultDto>> GetSavedMappingsAsync(Guid organisationId, string fingerprint);

    /// <summary>Saves column mappings for a batch and advances status to 'preview'.</summary>
    Task SaveMappingsAsync(Guid batchId, SaveMappingsRequest request);

    /// <summary>Returns a preview of the first 10 rows with mapping applied.</summary>
    Task<ImportPreviewDto> PreviewAsync(Guid batchId);

    /// <summary>Executes the full import for a batch.</summary>
    Task ExecuteAsync(Guid batchId);

    /// <summary>Validates the batch is in preview state, transitions it to importing, and enqueues for background processing.</summary>
    Task EnqueueAsync(Guid batchId);

    /// <summary>
    /// Creates an import batch record from a file already in blob storage.
    /// Returns null if a batch for that blob path already exists (idempotent).
    /// </summary>
    Task<ImportBatchDto?> CreateBatchFromBlobAsync(
        Guid organisationId, int projectId,
        string containerName, string blobPath, string fileName, string uploadedBy);

    /// <summary>Returns paginated import history for an organisation.</summary>
    Task<PagedResult<ImportBatchDto>> GetBatchesAsync(Guid organisationId, int page = 1, int pageSize = 20);

    /// <summary>Returns a single import batch.</summary>
    Task<ImportBatchDto?> GetBatchAsync(Guid batchId);

    /// <summary>Returns error rows for a batch.</summary>
    Task<IEnumerable<ImportErrorDto>> GetErrorsAsync(Guid batchId);

    /// <summary>
    /// Sets a pending or preview batch status to 'cancelled'.
    /// Throws InvalidOperationException if the batch is actively importing.
    /// </summary>
    Task CancelAsync(Guid batchId);

    /// <summary>
    /// Permanently deletes a batch and all its mappings/errors from the database and filesystem.
    /// Throws InvalidOperationException if the batch is actively importing.
    /// </summary>
    Task DeleteAsync(Guid batchId);

    /// <summary>
    /// Reconstructs the upload-response shape from a stored batch so the mapping wizard
    /// can be resumed. For 'pending' batches re-runs auto-matching; for 'preview' batches
    /// loads the persisted column mappings.
    /// </summary>
    Task<UploadImportResponseDto> ResumeAsync(Guid batchId);

    /// <summary>
    /// Scans the uploaded file for distinct values in columns mapped to dropdown/multiselect
    /// fields and returns those not yet covered by known options or existing aliases.
    /// Used to drive the value-mapping wizard step.
    /// </summary>
    Task<ValueMappingResponseDto> GetValueMappingAsync(Guid batchId);

    /// <summary>
    /// Resets a completed/cancelled/failed batch back to an earlier status so it can be
    /// remapped or re-executed. Clears error rows and execution timestamps.
    /// targetStatus must be 'pending' (go back to column mapping) or 'preview' (re-execute only).
    /// </summary>
    Task ResetBatchAsync(Guid batchId, string targetStatus = "pending");
}


public interface IImportStagingService
{
    /// <summary>Returns all staged columns for an organisation.</summary>
    Task<IEnumerable<ImportColumnStagingDto>> GetByOrganisationIdAsync(Guid organisationId, string? status = null);

    /// <summary>Returns a single staging record.</summary>
    Task<ImportColumnStagingDto?> GetByIdAsync(Guid stagingId);

    /// <summary>Resolves or skips a staged column.</summary>
    Task<ImportColumnStagingDto> ResolveAsync(Guid stagingId, ResolveColumnStagingRequest request);

    /// <summary>Deletes a staging record.</summary>
    Task DeleteAsync(Guid stagingId);
}


public interface IDashboardService
{
    /// <summary>Returns summary statistics across all active organisations.</summary>
    Task<DashboardStatsDto> GetStatsAsync(int warningDays);

    /// <summary>Returns active projects approaching their end date.</summary>
    Task<IEnumerable<ExpiringProjectDto>> GetExpiringProjectsAsync(int warningDays);
}


public interface ILibraryService
{
    Task<IEnumerable<LibrarySectionDto>> GetAllSectionsAsync(bool includeInactive = false);
    Task<LibrarySectionDto?> GetSectionByIdAsync(Guid sectionId);
    Task<LibrarySectionDto> CreateSectionAsync(CreateLibrarySectionRequest request);
    Task<LibrarySectionDto> UpdateSectionAsync(Guid sectionId, UpdateLibrarySectionRequest request);
    Task SetSectionStatusAsync(Guid sectionId, bool isActive);
    Task AssignFieldsToSectionAsync(Guid sectionId, AssignLibraryFieldsRequest request);

    Task<IEnumerable<LibraryFieldDto>> GetAllFieldsAsync(bool includeInactive = false);
    Task<LibraryFieldDto?> GetFieldByIdAsync(Guid fieldId);
    Task<LibraryFieldDto> CreateFieldAsync(CreateLibraryFieldRequest request);
    Task<LibraryFieldDto> UpdateFieldAsync(Guid fieldId, UpdateLibraryFieldRequest request);
    Task SetFieldStatusAsync(Guid fieldId, bool isActive);
    Task BulkUpsertOptionsAsync(Guid fieldId, BulkUpsertFieldOptionsRequest request);

    /// <summary>Copies the selected library sections (with fields and options) into an org as live FieldSections/FieldDefinitions/FieldOptions.</summary>
    Task<ImportFromLibraryResult> ImportToOrgAsync(ImportFromLibraryRequest request);
}


public interface ICustomerAddressService
{
    /// <summary>Returns all addresses for a customer, newest first.</summary>
    Task<IEnumerable<CustomerAddressDto>> GetAllAsync(Guid customerId);

    /// <summary>Returns the customer's current address, or null.</summary>
    Task<CustomerAddressDto?> GetCurrentAsync(Guid customerId);

    /// <summary>
    /// Validates the address via Melissa, then saves it as the customer's
    /// current address (previous address is preserved in history).
    /// </summary>
    Task<CustomerAddressDto> CreateAsync(Guid customerId, CreateCustomerAddressRequest request);

    /// <summary>Marks the address as confirmed by the customer.</summary>
    Task<CustomerAddressDto> ConfirmAsync(Guid customerId, Guid addressId);
}


public interface IMelissaService
{
    /// <summary>
    /// Submits an address to Melissa for verification and standardisation.
    /// Returns the result including whether the address is valid and the
    /// standardised components. Never throws — returns IsValid=false on failure.
    /// </summary>
    Task<MelissaValidationResult> ValidateAsync(
        string addressLine1, string? addressLine2,
        string city, string state, string postalCode, string country = "US");
}

public interface IContractDocumentService
{
    Task<IEnumerable<ContractDocumentDto>> GetByContractIdAsync(Guid contractId);
    Task<ContractDocumentDto?> GetByIdAsync(Guid documentId);
    /// <summary>Saves file to disk and records metadata. Returns the created DTO.</summary>
    Task<ContractDocumentDto> UploadAsync(Guid contractId, IFormFile file, UploadContractDocumentRequest request);
    /// <summary>Returns the file stream and its metadata for serving to the client. Throws KeyNotFoundException if not found.</summary>
    Task<(Stream FileStream, ContractDocumentDto Metadata)> DownloadAsync(Guid documentId);
    /// <summary>Deletes the DB record and the file on disk.</summary>
    Task DeleteAsync(Guid documentId);
}

public interface IOrganizationStorageService
{
    /// <summary>
    /// Creates a blob container for the organization if it does not already exist.
    /// Container name: org-{abbreviation} (lowercase, alphanumeric+hyphen) or org-{orgId prefix} as fallback.
    /// </summary>
    Task ProvisionContainerAsync(Guid organisationId, string? abbreviation);

    /// <summary>Returns the Azure Blob container name for an org (pure name-building, no I/O).</summary>
    string GetContainerName(Guid organisationId, string? abbreviation);

    /// <summary>
    /// Ensures the container exists, then uploads content to {feature}/{blobName}.
    /// Returns the blob path within the container (e.g. "Contracts/ADX_20250508143022_doc.pdf").
    /// </summary>
    Task<string> UploadFileAsync(string containerName, string feature, string blobName, Stream content, string contentType);

    /// <summary>Opens a read stream for the blob at the given path within the named container.</summary>
    Task<Stream> DownloadFileAsync(string containerName, string blobPath);

    /// <summary>Deletes the blob. No-ops if the blob does not exist.</summary>
    Task DeleteFileAsync(string containerName, string blobPath);

    /// <summary>
    /// Creates a zero-byte .keep placeholder at imports/{projectId}/.keep, materialising the
    /// virtual SFTP drop-zone folder for the project. Idempotent — safe to call on every project create.
    /// </summary>
    Task ProvisionProjectFolderAsync(string containerName, int projectId);

    /// <summary>Yields the name of every blob under the given prefix in the container.</summary>
    IAsyncEnumerable<string> ListBlobsAsync(string containerName, string prefix);
}

public interface IIngestionJobService
{
    Task<IngestionJobDto> CreateJobAsync(Guid organizationId, IFormFile file, string uploadedBy);
    Task<IngestionJobDto?> GetJobAsync(Guid jobId);
    Task<PagedResult<IngestionJobDto>> GetJobsAsync(Guid organizationId, int page, int pageSize);
    Task<PagedResult<IngestionStagingRowDto>> GetStagingRowsAsync(Guid jobId, string? statusFilter, int page, int pageSize);
    Task ApproveRowAsync(Guid jobId, Guid rowId, string reviewedBy);
    Task RejectRowAsync(Guid jobId, Guid rowId, string reviewedBy, string? reason);
    Task CommitJobAsync(Guid jobId, string committedBy);
}

public interface IFieldOptionAliasService
{
    Task<IEnumerable<FieldOptionAliasDto>> GetByOrganizationAsync(Guid organizationId);
    Task<IEnumerable<FieldOptionAliasDto>> BulkSaveAsync(Guid organizationId, BulkSaveAliasesRequest request);
    Task DeleteAsync(Guid id);
}