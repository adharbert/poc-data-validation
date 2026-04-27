namespace POC.CustomerValidation.API.Models.Entites;

public class ImportBatch
{
    public Guid         BatchId             { get; set; }
    public Guid         OrganizationId      { get; set; }
    public string       FileName            { get; set; } = default!;
    public string?      FileType            { get; set; }           // csv, xlsx, xls
    public string       FileHeaders         { get; set; } = default!; // JSON array
    public string       HeaderFingerprint   { get; set; } = default!; // SHA-256 hex
    public string?      FileStoragePath     { get; set; }
    public int          TotalRows           { get; set; }
    public int          ImportedRows        { get; set; }
    public int          SkippedRows         { get; set; }
    public int          ErrorRows           { get; set; }
    public string       Status              { get; set; } = "pending";
    public string       DuplicateStrategy   { get; set; } = "skip";
    public string       UploadedBy          { get; set; } = default!;
    public DateTime     UploadedAt          { get; set; }
    public DateTime?    MappingSavedAt      { get; set; }
    public DateTime?    ExecutionStartedAt  { get; set; }
    public DateTime?    CompletedAt         { get; set; }
    public string?      Notes               { get; set; }
}
