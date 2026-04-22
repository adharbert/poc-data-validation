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
    DateTime    CreatedAt,
    string      CreatedBy,
    DateTime    UpdatedAt,
    string?     ModifiedBy
);

public record CreateOrganizationRequest(
    Guid    OrganizationId,
    string  OrganizationName,
    string  OrganizationCode,
    string? FilingName,
    string? MarketingName,
    string? Abbreviation,
    string? Website,
    string? Phone,
    string? CompanyEmail
);

public record UpdateOrganizationRequest(
    Guid OrganizationId,
    string  OrganizationName,
    string  OrganizationCode,
    string? FilingName,
    string? MarketingName,
    string? Abbreviation,
    string? Website,
    string? Phone,
    string? CompanyEmail,
    bool?   IsActive
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
    string?     RegexPattern    = null
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
    string?     RegexPattern
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







