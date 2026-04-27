namespace POC.CustomerValidation.API.Models.Entites;

public class SavedColumnMapping
{
    public Guid     SavedMappingId      { get; set; }
    public Guid     OrganizationId      { get; set; }
    public string   HeaderFingerprint   { get; set; } = default!;
    public string   CsvHeader           { get; set; } = default!;
    public int      CsvColumnIndex      { get; set; }
    public string   MappingType         { get; set; } = default!;
    public string?  CustomerFieldName   { get; set; }
    public Guid?    FieldDefinitionId   { get; set; }
    public int      DisplayOrder        { get; set; }
    public DateTime LastUsedAt          { get; set; }
    public int      UseCount            { get; set; }
}
