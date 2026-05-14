namespace POC.CustomerValidation.API.Models.Entites;

public class ImportColumnMappingOutput
{
    public Guid     OutputId            { get; set; }
    public Guid     MappingId           { get; set; }
    public string   OutputToken         { get; set; } = default!;  // e.g. FirstName, MiddleName, LastName, Suffix, Credentials
    public string   DestinationTable    { get; set; } = "skip";
    public string?  DestinationField    { get; set; }
    public Guid?    FieldDefinitionId   { get; set; }
    public int      SortOrder           { get; set; }
}
