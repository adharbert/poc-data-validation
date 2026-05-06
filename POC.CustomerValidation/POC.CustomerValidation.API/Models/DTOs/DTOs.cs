namespace POC.CustomerValidation.API.Models.DTOs;

// -------------------------------------------------------
// Organization DTOs
// -------------------------------------------------------
public record OrganizationDto(
    Guid        OrganizationId,
    string      OrganizationName,
    string      OrganizationCode,
    string?     FilingName,
    string?     MarketingName,
    string?     Abbreviation,
    string?     Website,
    string?     Phone,
    string?     CompanyEmail,
    bool        IsActive,
    bool        RequiresIsolatedDatabase,
    string?     DatabaseProvisioningStatus,
    DateTime    CreatedAt,
    string      CreatedBy,
    DateTime    UpdatedAt,
    string?     ModifiedBy
);

public record CreateOrganizationRequest(
    string  OrganizationName,
    string? FilingName,
    string? MarketingName,
    string? Abbreviation,
    string? Website,
    string? Phone,
    string? CompanyEmail,
    bool    RequiresIsolatedDatabase = false
);

public record UpdateOrganizationRequest(
    Guid    OrganizationId,
    string  OrganizationName,
    string? FilingName,
    string? MarketingName,
    string? Abbreviation,
    string? Website,
    string? Phone,
    string? CompanyEmail,
    bool?   IsActive,
    bool    RequiresIsolatedDatabase = false
);



// -------------------------------------------------------
// Field Sections
// -------------------------------------------------------

public record FieldSectionDto(
    Guid    SectionId,
    Guid    OrganizationId,
    string  SectionName,
    int     DisplayOrder,
    bool    IsActive
);

public record CreateFieldSectionRequest(
    Guid    OrganizationId,
    string  SectionName,
    int     DisplayOrder = 0
);

public record UpdateFieldSectionRequest(
    string  SectionName,
    int     DisplayOrder,
    bool    IsActive
);


public record ReorderFieldRequest(Guid FieldDefinitionId, int DisplayOrder);

public record ReorderSectionsRequest
{
    public IEnumerable<SectionOrderItem> Sections { get; init; } = [];
}

public record SectionOrderItem
{
    public Guid SectionId    { get; init; }
    public int  DisplayOrder { get; init; }
}

public record AssignFieldsToSectionRequest
{
    public IEnumerable<SectionFieldAssignment> Fields { get; init; } = [];
}

public record SectionFieldAssignment
{
    public Guid FieldDefinitionId { get; init; }
    public int  DisplayOrder      { get; init; }
}

// -------------------------------------------------------
// Form Preview DTOs
// -------------------------------------------------------

public record CustomerFormPreviewDto
{
    public Guid                                 CustomerId      { get; init; }
    public string                               CustomerName    { get; init; } = default!;
    public IEnumerable<SectionPreviewDto>       Sections        { get; init; } = [];
    public IEnumerable<FieldPreviewItemDto>     UnassignedFields { get; init; } = [];
}

public record SectionPreviewDto
{
    public Guid                             SectionId       { get; init; }
    public string                           SectionName     { get; init; } = default!;
    public int                              DisplayOrder    { get; init; }
    public IEnumerable<FieldPreviewItemDto> Fields          { get; init; } = [];
}

public record FieldPreviewItemDto
{
    public Guid     FieldDefinitionId   { get; init; }
    public Guid?    SectionId           { get; init; }
    public string   FieldKey            { get; init; } = default!;
    public string   FieldLabel          { get; init; } = default!;
    public string   FieldType           { get; init; } = default!;
    public string?  HelpText            { get; init; }
    public bool     IsRequired          { get; init; }
    public int      DisplayOrder        { get; init; }
    public string?  CurrentValue        { get; init; }
    public string?  DisplayFormat       { get; init; }
    public IEnumerable<FieldOptionDto>  Options { get; init; } = [];
}

// -------------------------------------------------------
// Field Definitions
// -------------------------------------------------------

public record FieldDefinitionDto(
    Guid        FieldDefinitionId,
    Guid        OrganizationId,
    Guid?       SectionId,
    string?     SectionName,
    string      FieldKey,
    string      FieldLabel,
    string      FieldType,
    string?     PlaceholderText,
    string?     HelpText,
    bool        IsRequired,
    bool        IsActive,
    int         DisplayOrder, 
    decimal?    MinValue,
    decimal?    MaxValue,
    int?        MinLength,
    int?        MaxLength,
    string?     RegexPattern,
    string?     DisplayFormat,
    IEnumerable<FieldOptionDto> Options
);



public record CreateFieldDefinitionRequest(
    Guid        OrganizationId,
    Guid?       SectionId,
    string      FieldKey,
    string      FieldLabel,
    string      FieldType,
    string?     PlaceholderText,
    string?     HelpText,
    bool        IsRequired      = false,
    int         DisplayOrder    = 0,
    decimal?    MinValue        = null,
    decimal?    MaxValue        = null,
    int?        MinLength       = null,
    int?        MaxLength       = null,
    string?     RegexPattern    = null,
    string?     DisplayFormat   = null
);


public record UpdateFieldDefinitionRequest(
    Guid        FieldDefinitionId,
    Guid?       SectionId,
    string      FieldLabel,
    string      FieldType,
    string?     PlaceholderText,
    string?     HelpText,
    bool        IsRequired,
    bool        IsActive,
    int         DisplayOrder,
    decimal?    MinValue,
    decimal?    MaxValue,
    int?        MinLength,
    int?        MaxLength,
    string?     RegexPattern,
    string?     DisplayFormat
);




// -------------------------------------------------------
// Field Options
// -------------------------------------------------------

public record FieldOptionDto(
    Guid        OptionId,
    Guid        FieldDefinitionId,
    string      OptionKey,
    string      OptionLabel,
    int         DisplayOrder,
    bool        IsActive
);


public record CreateFieldOptionRequest(
    string  OptionKey,
    string  OptionLabel,
    int     DisplayOrder = 0
);

public record UpdateFieldOptionRequest(
    string  OptionKey,
    string  OptionLabel,
    int     DisplayOrder,
    bool    IsActive
);

public record BulkUpsertFieldOptionsRequest(
    IEnumerable<CreateFieldOptionRequest> Options
);



// -------------------------------------------------------
// Field Values
// -------------------------------------------------------
public record FieldValueDto(
    Guid FieldValueId,
    Guid CustomerId,
    Guid FieldDefinitionId,
    string FieldLabel,
    string FieldType,
    string? ValueText,
    decimal? ValueNumber,
    DateOnly? ValueDate,
    DateTime? ValueDatetime,
    bool? ValueBoolean,
    string? DisplayValue,
    DateTime? ConfirmedAt,
    string? ConfirmedBy,
    DateTime? FlaggedAt,
    string? FlagNote,
    DateTime CreatedDt,
    DateTime ModifiedDt
);


public record FieldValueHistoryDto(
    Guid HistoryId,
    Guid FieldValueId,
    Guid FieldDefinitionId,
    Guid CustomerId,
    string FieldLabel,
    string? OldValue,
    string? ChangedBy,
    DateTime ChangedAt,
    string? ChangeReason
);







// -------------------------------------------------------
// Shared DTOs
// -------------------------------------------------------
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record ApiError(
    string Code,
    string Message,
    Dictionary<string, string[]>? ValidationErrors = null
);


// -------------------------------------------------------
// Customer DTOs
// -------------------------------------------------------

public record CustomerDto
{
    public Guid      CustomerId      { get; init; }
    public Guid      OrganizationId  { get; init; }
    public string    FirstName       { get; init; } = default!;
    public string    LastName        { get; init; } = default!;
    public string?   MiddleName      { get; init; }
    public string?   MaidenName      { get; init; }
    public DateOnly? DateOfBirth     { get; init; }
    public string    CustomerCode    { get; init; } = default!;
    public string?   OriginalId      { get; init; }
    public string?   Email           { get; init; }
    public string?   Phone           { get; init; }
    public bool      IsActive        { get; init; }
    public DateTime  CreatedDate     { get; init; }
    public DateTime  ModifiedDate    { get; init; }
}

public record CreateCustomerRequest
{
    public string    FirstName   { get; init; } = default!;
    public string    LastName    { get; init; } = default!;
    public string?   MiddleName  { get; init; }
    public string?   MaidenName  { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string?   OriginalId  { get; init; }
    public string?   Email       { get; init; }
    public string?   Phone       { get; init; }
}

public record UpdateCustomerRequest
{
    public string    FirstName   { get; init; } = default!;
    public string    LastName    { get; init; } = default!;
    public string?   MiddleName  { get; init; }
    public string?   MaidenName  { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string?   OriginalId  { get; init; }
    public string?   Email       { get; init; }
    public string?   Phone       { get; init; }
    public bool      IsActive    { get; init; }
}


// -------------------------------------------------------
// Contract DTOs
// -------------------------------------------------------

public record ContractDto
{
    public Guid         ContractId      { get; init; }
    public Guid         OrganizationId  { get; init; }
    public string       ContractName    { get; init; } = default!;
    public string?      ContractNumber  { get; init; }
    public DateOnly     StartDate       { get; init; }
    public DateOnly?    EndDate         { get; init; }
    public bool         IsActive        { get; init; }
    public string?      Notes           { get; init; }
    public DateTime     CreatedDt       { get; init; }
    public string       CreatedBy       { get; init; } = default!;
    public DateTime?    ModifiedDt      { get; init; }
    public string?      ModifiedBy      { get; init; }
}

public record CreateContractRequest
{
    public string       ContractName    { get; init; } = default!;
    public string?      ContractNumber  { get; init; }
    public DateOnly     StartDate       { get; init; }
    public DateOnly?    EndDate         { get; init; }
    public string?      Notes           { get; init; }
    public string       CreatedBy       { get; init; } = "System";
}

public record UpdateContractRequest
{
    public string       ContractName    { get; init; } = default!;
    public string?      ContractNumber  { get; init; }
    public DateOnly     StartDate       { get; init; }
    public DateOnly?    EndDate         { get; init; }
    public string?      Notes           { get; init; }
    public string       ModifiedBy      { get; init; } = "System";
}


// -------------------------------------------------------
// Marketing Project DTOs
// -------------------------------------------------------

public record MarketingProjectDto
{
    public int          ProjectId           { get; init; }
    public Guid         OrganizationId      { get; init; }
    public string       OrganizationName    { get; init; } = default!;
    public Guid?        ContractId          { get; init; }
    public string       ProjectName         { get; init; } = default!;
    public DateOnly     MarketingStartDate  { get; init; }
    public DateOnly?    MarketingEndDate    { get; init; }
    public bool         IsActive            { get; init; }
    public string?      Notes               { get; init; }
    public DateTime     CreatedDt           { get; init; }
    public string       CreatedBy           { get; init; } = default!;
    public DateTime?    ModifiedDt          { get; init; }
    public string?      ModifiedBy          { get; init; }
}

public record CreateMarketingProjectRequest
{
    public Guid?        ContractId          { get; init; }
    public string       ProjectName         { get; init; } = default!;
    public DateOnly     MarketingStartDate  { get; init; }
    public DateOnly?    MarketingEndDate    { get; init; }
    public string?      Notes               { get; init; }
    public string       CreatedBy           { get; init; } = "System";
}

public record UpdateMarketingProjectRequest
{
    public Guid?        ContractId          { get; init; }
    public string       ProjectName         { get; init; } = default!;
    public DateOnly     MarketingStartDate  { get; init; }
    public DateOnly?    MarketingEndDate    { get; init; }
    public bool         IsActive            { get; init; }
    public string?      Notes               { get; init; }
    public string       ModifiedBy          { get; init; } = "System";
}


// -------------------------------------------------------
// Dashboard DTOs
// -------------------------------------------------------

public record DashboardStatsDto
{
    public int                                  TotalActiveOrganizations    { get; init; }
    public int                                  TotalActiveProjects         { get; init; }
    public int                                  TotalCustomers              { get; init; }
    public int                                  TotalVerifiedCustomers      { get; init; }
    public IEnumerable<OrganisationSummaryDto>  OrganisationSummaries       { get; init; } = [];
    public IEnumerable<ExpiringProjectDto>      ExpiringProjects            { get; init; } = [];
}

public record OrganisationSummaryDto
{
    public Guid     OrganisationId      { get; init; }
    public string   OrganisationName    { get; init; } = default!;
    public int      TotalCustomers      { get; init; }
    public int      VerifiedCustomers   { get; init; }
    public int      ActiveProjects      { get; init; }
}

public record ExpiringProjectDto
{
    public int      ProjectId           { get; init; }
    public string   ProjectName         { get; init; } = default!;
    public Guid     OrganisationId      { get; init; }
    public string   OrganisationName    { get; init; } = default!;
    public DateOnly MarketingEndDate    { get; init; }
    public int      DaysUntilExpiry     { get; init; }
}


// -------------------------------------------------------
// Import DTOs
// -------------------------------------------------------

public record ImportBatchDto
{
    public Guid         BatchId             { get; init; }
    public Guid         OrganizationId      { get; init; }
    public string       FileName            { get; init; } = default!;
    public string?      FileType            { get; init; }
    public int          TotalRows           { get; init; }
    public int          ImportedRows        { get; init; }
    public int          SkippedRows         { get; init; }
    public int          ErrorRows           { get; init; }
    public string       Status              { get; init; } = default!;
    public string       DuplicateStrategy   { get; init; } = default!;
    public string       UploadedBy          { get; init; } = default!;
    public DateTime     UploadedAt          { get; init; }
    public DateTime?    CompletedAt         { get; init; }
    public string?      Notes               { get; init; }
}

public record UploadImportResponseDto
{
    public Guid                              BatchId              { get; init; }
    public string[]                          Headers              { get; init; } = [];
    public int                               RowCount             { get; init; }
    public bool                              HasSavedMappings     { get; init; }
    public IEnumerable<ColumnMatchResultDto> ColumnMatches        { get; init; } = [];
    /// <summary>True when saved mappings exist but expected columns are missing from this upload.</summary>
    public bool                              SchemaDrift          { get; init; }
    /// <summary>Columns that were in the previous saved mapping but are absent from this file.</summary>
    public string[]                          MissingMappedColumns { get; init; } = [];
    /// <summary>Columns present in this file that were not in the previous saved mapping.</summary>
    public string[]                          NewColumns           { get; init; } = [];
}

public record ColumnMappingOutputDto
{
    public string   OutputToken         { get; init; } = default!;
    public string   DestinationTable    { get; init; } = "skip";
    public string?  DestinationField    { get; init; }
    public Guid?    FieldDefinitionId   { get; init; }
    public int      SortOrder           { get; init; }
}

public record ColumnMatchResultDto
{
    public int      ColumnIndex         { get; init; }
    public string   CsvHeader           { get; init; } = default!;
    public string   MatchStatus         { get; init; } = default!;  // matched | unmatched | skipped
    public string?  DestinationTable    { get; init; }
    public string?  DestinationField    { get; init; }
    public Guid?    FieldDefinitionId   { get; init; }
    public string?  TransformType       { get; init; }
    public string?  FieldLabel          { get; init; }
    public bool     IsAutoMatched       { get; init; }
    public IEnumerable<ColumnMappingOutputDto> Outputs { get; init; } = [];
}

public record SaveMappingsRequest
{
    public string                           DuplicateStrategy   { get; init; } = "skip";
    public IEnumerable<ColumnMappingDto>    Mappings            { get; init; } = [];
}

public record ColumnMappingDto
{
    public int      ColumnIndex         { get; init; }
    public string   CsvHeader           { get; init; } = default!;
    public string   DestinationTable    { get; init; } = "skip";
    public string?  DestinationField    { get; init; }
    public Guid?    FieldDefinitionId   { get; init; }
    public string   TransformType       { get; init; } = "direct";
    public bool     SaveForReuse        { get; init; } = true;
    public IEnumerable<ColumnMappingOutputDto> Outputs { get; init; } = [];
}

public record ImportPreviewDto
{
    public IEnumerable<string>          Headers         { get; init; } = [];
    public IEnumerable<PreviewRowDto>   Rows            { get; init; } = [];
    public int                          OkCount         { get; init; }
    public int                          WarningCount    { get; init; }
    public int                          ErrorCount      { get; init; }
}

public record PreviewRowDto
{
    public int                      RowNumber   { get; init; }
    public IEnumerable<string?>     Values      { get; init; } = [];
    public string                   Status      { get; init; } = "ok";  // ok | warning | error
    public string?                  Message     { get; init; }
}

public record ImportErrorDto
{
    public Guid     ErrorId         { get; init; }
    public int      RowNumber       { get; init; }
    public string   ErrorType       { get; init; } = default!;
    public string   ErrorMessage    { get; init; } = default!;
    public string   RawData         { get; init; } = default!;
}


// -------------------------------------------------------
// Import Column Staging DTOs
// -------------------------------------------------------

public record ImportColumnStagingDto
{
    public Guid         StagingId           { get; init; }
    public Guid         OrganizationId      { get; init; }
    public string       CsvHeader           { get; init; } = default!;
    public string       Status              { get; init; } = default!;
    public string?      MappingType         { get; init; }
    public string?      CustomerFieldName   { get; init; }
    public Guid?        FieldDefinitionId   { get; init; }
    public string?      FieldLabel          { get; init; }
    public int          SeenCount           { get; init; }
    public DateTime     FirstSeenAt         { get; init; }
    public DateTime     LastSeenAt          { get; init; }
    public DateTime?    ResolvedAt          { get; init; }
    public string?      ResolvedBy          { get; init; }
    public string?      Notes               { get; init; }
}

public record ResolveColumnStagingRequest
{
    public string   Status              { get; init; } = default!;  // resolved | skipped
    public string?  MappingType         { get; init; }
    public string?  CustomerFieldName   { get; init; }
    public Guid?    FieldDefinitionId   { get; init; }
    public string?  Notes               { get; init; }
    public string   ResolvedBy          { get; init; } = "System";
}


// -------------------------------------------------------
// Customer Address DTOs
// -------------------------------------------------------

public record CustomerAddressDto
{
    public Guid     AddressId           { get; init; }
    public Guid     CustomerId          { get; init; }
    public string   AddressLine1        { get; init; } = default!;
    public string?  AddressLine2        { get; init; }
    public string   City                { get; init; } = default!;
    public string   State               { get; init; } = default!;
    public string   PostalCode          { get; init; } = default!;
    public string   Country             { get; init; } = "US";
    public string   AddressType         { get; init; } = "primary";
    public double?  Latitude            { get; init; }
    public double?  Longitude           { get; init; }
    public bool     MelissaValidated    { get; init; }
    public bool     CustomerConfirmed   { get; init; }
    public bool     IsCurrent           { get; init; }
    public DateTime CreatedUtcDt        { get; init; }
    public DateTime ModifiedUtcDt       { get; init; }
}

public record CreateCustomerAddressRequest
{
    public string   AddressLine1    { get; init; } = default!;
    public string?  AddressLine2    { get; init; }
    public string   City            { get; init; } = default!;
    public string   State           { get; init; } = default!;
    public string   PostalCode      { get; init; } = default!;
    public string   Country         { get; init; } = "US";
    public string   AddressType     { get; init; } = "primary";
    public double?  Latitude        { get; init; }
    public double?  Longitude       { get; init; }
}

// -------------------------------------------------------
// Customer Phone DTOs
// -------------------------------------------------------

public record CustomerPhoneDto
{
    public Guid     PhoneId         { get; init; }
    public Guid     CustomerId      { get; init; }
    public string   PhoneNumber     { get; init; } = default!;
    public string   PhoneType       { get; init; } = default!;
    public bool     IsPrimary       { get; init; }
    public bool     IsActive        { get; init; }
    public DateTime CreatedUtcDt    { get; init; }
    public DateTime ModifiedUtcDt   { get; init; }
}

public record CreateCustomerPhoneRequest
{
    public string   PhoneNumber     { get; init; } = default!;
    public string   PhoneType       { get; init; } = "mobile";
    public bool     IsPrimary       { get; init; }
}

public record UpdateCustomerPhoneRequest
{
    public string   PhoneNumber     { get; init; } = default!;
    public string   PhoneType       { get; init; } = default!;
    public bool     IsPrimary       { get; init; }
    public bool     IsActive        { get; init; }
}

// -------------------------------------------------------
// Customer Email DTOs
// -------------------------------------------------------

public record CustomerEmailDto
{
    public Guid     EmailId         { get; init; }
    public Guid     CustomerId      { get; init; }
    public string   EmailAddress    { get; init; } = default!;
    public string   EmailType       { get; init; } = default!;
    public bool     IsPrimary       { get; init; }
    public bool     IsActive        { get; init; }
    public DateTime CreatedUtcDt    { get; init; }
    public DateTime ModifiedUtcDt   { get; init; }
}

public record CreateCustomerEmailRequest
{
    public string   EmailAddress    { get; init; } = default!;
    public string   EmailType       { get; init; } = "personal";
    public bool     IsPrimary       { get; init; }
}


// -------------------------------------------------------
// Field Library DTOs
// -------------------------------------------------------

public record LibraryFieldOptionDto
{
    public Guid     Id              { get; init; }
    public Guid     LibraryFieldId  { get; init; }
    public string   OptionKey       { get; init; } = default!;
    public string   OptionLabel     { get; init; } = default!;
    public int      DisplayOrder    { get; init; }
    public bool     IsActive        { get; init; }
}

public record LibraryFieldDto
{
    public Guid     Id              { get; init; }
    public string   FieldKey        { get; init; } = default!;
    public string   FieldLabel      { get; init; } = default!;
    public string   FieldType       { get; init; } = default!;
    public string?  PlaceHolderText { get; init; }
    public string?  HelpText        { get; init; }
    public bool     IsRequired      { get; init; }
    public int      DisplayOrder    { get; init; }
    public decimal? MinValue        { get; init; }
    public decimal? MaxValue        { get; init; }
    public int?     MinLength       { get; init; }
    public int?     MaxLength       { get; init; }
    public string?  RegExPattern    { get; init; }
    public string?  DisplayFormat   { get; init; }
    public bool     IsActive        { get; init; }
    public IEnumerable<LibraryFieldOptionDto> Options { get; init; } = [];
}

public record LibrarySectionDto
{
    public Guid     Id              { get; init; }
    public string   SectionName     { get; init; } = default!;
    public string?  Description     { get; init; }
    public int      DisplayOrder    { get; init; }
    public bool     IsActive        { get; init; }
    public IEnumerable<LibraryFieldDto> Fields { get; init; } = [];
}

public record CreateLibrarySectionRequest
{
    public string   SectionName     { get; init; } = default!;
    public string?  Description     { get; init; }
    public int      DisplayOrder    { get; init; }
}

public record UpdateLibrarySectionRequest
{
    public string   SectionName     { get; init; } = default!;
    public string?  Description     { get; init; }
    public int      DisplayOrder    { get; init; }
    public bool     IsActive        { get; init; }
}

public record AssignLibraryFieldsRequest
{
    public IEnumerable<LibraryFieldOrderItem> Fields { get; init; } = [];
}

public record LibraryFieldOrderItem
{
    public Guid LibraryFieldId  { get; init; }
    public int  DisplayOrder    { get; init; }
}

public record CreateLibraryFieldRequest
{
    public string   FieldKey        { get; init; } = default!;
    public string   FieldLabel      { get; init; } = default!;
    public string   FieldType       { get; init; } = default!;
    public string?  PlaceHolderText { get; init; }
    public string?  HelpText        { get; init; }
    public bool     IsRequired      { get; init; }
    public int      DisplayOrder    { get; init; }
    public decimal? MinValue        { get; init; }
    public decimal? MaxValue        { get; init; }
    public int?     MinLength       { get; init; }
    public int?     MaxLength       { get; init; }
    public string?  RegExPattern    { get; init; }
    public string?  DisplayFormat   { get; init; }
}

public record UpdateLibraryFieldRequest
{
    public string   FieldLabel      { get; init; } = default!;
    public string   FieldType       { get; init; } = default!;
    public string?  PlaceHolderText { get; init; }
    public string?  HelpText        { get; init; }
    public bool     IsRequired      { get; init; }
    public int      DisplayOrder    { get; init; }
    public decimal? MinValue        { get; init; }
    public decimal? MaxValue        { get; init; }
    public int?     MinLength       { get; init; }
    public int?     MaxLength       { get; init; }
    public string?  RegExPattern    { get; init; }
    public string?  DisplayFormat   { get; init; }
    public bool     IsActive        { get; init; }
}

public record ImportFromLibraryRequest
{
    public Guid                 OrganizationId  { get; init; }
    public IEnumerable<Guid>    SectionIds      { get; init; } = [];
}

public record ImportFromLibraryResult
{
    public int  SectionsCreated { get; init; }
    public int  FieldsCreated   { get; init; }
    public int  OptionsCreated  { get; init; }
}

public record UpdateCustomerEmailRequest
{
    public string   EmailAddress    { get; init; } = default!;
    public string   EmailType       { get; init; } = default!;
    public bool     IsPrimary       { get; init; }
    public bool     IsActive        { get; init; }
}

// -------------------------------------------------------
// Melissa address-validation result
// -------------------------------------------------------

public record MelissaValidationResult
{
    public bool     IsValid         { get; init; }
    public string   AddressLine1    { get; init; } = default!;
    public string?  AddressLine2    { get; init; }
    public string   City            { get; init; } = default!;
    public string   State           { get; init; } = default!;
    public string   PostalCode      { get; init; } = default!;
}







public record SetStatusRequest
{
    public bool IsActive { get; init; }
    public string ModifiedBy { get; init; } = "System";
}