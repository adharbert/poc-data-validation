namespace POC.CustomerValidation.API.Models.Entites;

public class ImportError
{
    public Guid     ErrorId         { get; set; }
    public Guid     ImportBatchId   { get; set; }
    public int      RowNumber       { get; set; }
    public string   RawData         { get; set; } = default!; // original row as JSON
    public string   ErrorType       { get; set; } = "validation"; // validation | duplicate | system
    public string   ErrorMessage    { get; set; } = default!;
    public DateTime CreatedAt       { get; set; }
}
