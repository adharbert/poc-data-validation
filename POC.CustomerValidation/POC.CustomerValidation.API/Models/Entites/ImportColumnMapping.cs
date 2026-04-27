namespace POC.CustomerValidation.API.Models.Entites;

public class ImportColumnMapping
{
    public Guid     MappingId           { get; set; }
    public Guid     ImportBatchId       { get; set; }
    public string   CsvHeader           { get; set; } = default!;
    public int      CsvColumnIndex      { get; set; }
    public string   MappingType         { get; set; } = "skip"; // customer_field | field_definition | skip
    public string?  CustomerFieldName   { get; set; }
    public Guid?    FieldDefinitionId   { get; set; }
    public bool     IsAutoMatched       { get; set; }
    public bool     IsRequired          { get; set; }
    public bool     SavedForReuse       { get; set; } = true;
    public int      DisplayOrder        { get; set; }
}
