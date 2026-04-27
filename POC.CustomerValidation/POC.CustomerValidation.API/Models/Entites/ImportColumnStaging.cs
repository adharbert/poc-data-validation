namespace POC.CustomerValidation.API.Models.Entites;

public class ImportColumnStaging
{
    public Guid         StagingId           { get; set; }
    public Guid         OrganizationId      { get; set; }
    public string       CsvHeader           { get; set; } = default!;
    public string       HeaderNormalized    { get; set; } = default!;
    public string       Status              { get; set; } = "unmatched"; // unmatched | resolved | skipped
    public string?      MappingType         { get; set; }
    public string?      CustomerFieldName   { get; set; }
    public Guid?        FieldDefinitionId   { get; set; }
    public DateTime     FirstSeenAt         { get; set; }
    public DateTime     LastSeenAt          { get; set; }
    public int          SeenCount           { get; set; } = 1;
    public DateTime?    ResolvedAt          { get; set; }
    public string?      ResolvedBy          { get; set; }
    public string?      Notes               { get; set; }
}
