namespace POC.CustomerValidation.API.Models.Entites;

public class ImportColumnMapping
{
    public Guid     MappingId           { get; set; }
    public Guid     ImportBatchId       { get; set; }
    public string   CsvHeader           { get; set; } = default!;
    public int      CsvColumnIndex      { get; set; }
    public string   DestinationTable    { get; set; } = "skip";  // customer | customer_address | field_value | skip
    public string?  DestinationField    { get; set; }            // column name within the destination table
    public Guid?    FieldDefinitionId   { get; set; }            // used when DestinationTable = field_value
    public string   TransformType       { get; set; } = "direct"; // direct | split_full_name | split_full_address | strip_credentials
    public bool     IsAutoMatched       { get; set; }
    public bool     IsRequired          { get; set; }
    public bool     SavedForReuse       { get; set; } = true;
    public int      DisplayOrder        { get; set; }

    // Populated when TransformType != "direct"; empty for direct mappings
    public List<ImportColumnMappingOutput> Outputs { get; set; } = [];
}
