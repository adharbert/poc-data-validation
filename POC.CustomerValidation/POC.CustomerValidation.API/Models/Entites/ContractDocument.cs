namespace POC.CustomerValidation.API.Models.Entites;

public class ContractDocument
{
    public Guid         DocumentId      { get; set; }
    public Guid         ContractId      { get; set; }
    public Guid?        AmendmentId     { get; set; }   // null = contract-level document
    public string       OriginalFileName { get; set; } = default!;
    public string       StoredFileName  { get; set; } = default!;
    public string       StoragePath     { get; set; } = default!;
    public string       ContentType     { get; set; } = default!;
    public long         FileSizeBytes   { get; set; }
    public DateTime     UploadedAt      { get; set; }
    public string       UploadedBy      { get; set; } = default!;
}
