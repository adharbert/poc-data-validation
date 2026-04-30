namespace POC.CustomerValidation.API.Models.Entites;

public class FieldDefinition
{
    public Guid     FieldDefinitionId   { get; set; }
    public Guid     OrganizationId      { get; set; }
    public Guid?    FieldSectionId      { get; set; }
    public string   FieldKey            { get; set; } = default!;
    public string   FieldLabel          { get; set; } = default!;
    public string   FieldType           { get; set; } = default!;
    public string?  Placeholder         { get; set; }
    public string?  HelpText            { get; set; }
    public bool     IsRequired          { get; set; }
    public bool     IsActive            { get; set; } = true;
    public int      DisplayOrder        { get; set; }
    public decimal? MinValue            { get; set; }
    public decimal? MaxValue            { get; set; }
    public int?     MinLength           { get; set; }
    public int?     MaxLength           { get; set; }
    public string?  RegexPattern        { get; set; }
    public string?  DisplayFormat       { get; set; }
    public DateTime CreatedDt           { get; set; }
    public DateTime ModifiedDt          { get; set; }
}
