namespace POC.CustomerValidation.API.Models.Entites;

public class IngestionStagingRow
{
    public Guid         Id              { get; set; }
    public Guid         IngestionJobId  { get; set; }
    public int          RowNumber       { get; set; }
    public string       RowJson         { get; set; } = default!;   // normalized field values
    public decimal?     ConfidenceScore { get; set; }               // 0.0000–1.0000
    public string       Status          { get; set; } = "Pending";  // Pending|Pass|Flagged|Rejected|Committed
    public string?      FlagReasons     { get; set; }               // JSON array of reason strings
    public string?      ReviewedBy      { get; set; }
    public DateTime?    ReviewedAt      { get; set; }
}
