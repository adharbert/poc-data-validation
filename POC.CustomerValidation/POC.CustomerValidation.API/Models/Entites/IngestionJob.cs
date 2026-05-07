namespace POC.CustomerValidation.API.Models.Entites;

public class IngestionJob
{
    public Guid         Id                  { get; set; }
    public Guid         OrganizationId      { get; set; }
    public string       FileName            { get; set; } = default!;
    public string       FileType            { get; set; } = default!;   // csv, xlsx, xls
    public long         FileSizeBytes       { get; set; }
    public string       FileHash            { get; set; } = default!;   // SHA-256 hex
    public string?      FileStoragePath     { get; set; }
    public string       HeaderFingerprint   { get; set; } = default!;   // SHA-256 of sorted headers
    public string?      MappingJson         { get; set; }               // JSON column mapping array
    public string       UploadedBy          { get; set; } = default!;
    public DateTime     UploadedAt          { get; set; }
    public string       Status              { get; set; } = "Pending";
    public string?      Tier                { get; set; }               // Auto | Review | ETL
    public int?         TotalRows           { get; set; }
    public int?         PassedRows          { get; set; }
    public int?         FlaggedRows         { get; set; }
    public int?         FailedRows          { get; set; }
    public string?      ErrorMessage        { get; set; }
    public DateTime?    CompletedAt         { get; set; }
}
