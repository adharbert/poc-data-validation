namespace POC.CustomerValidation.API.Models.Entites;

public class FieldOptionAlias
{
    public Guid     Id                  { get; set; }
    public Guid     OrganizationId      { get; set; }
    public Guid     FieldDefinitionId   { get; set; }
    public string   AliasValue          { get; set; } = default!;
    public string   CanonicalValue      { get; set; } = default!;
    public DateTime CreatedDt           { get; set; }
    public DateTime ModifiedDt          { get; set; }
}
