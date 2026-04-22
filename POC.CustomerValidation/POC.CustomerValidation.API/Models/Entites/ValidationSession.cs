namespace POC.CustomerValidation.API.Models.Entites;

public class ValidationSession
{
    public Guid         SessionId       { get; set; }
    public Guid         CustomerId      { get; set; }
    public DateTime     StartedAt       { get; set; }
    public DateTime?    CompletedAt     { get; set; }
    public string       SessionStatus   { get; set; } = "in progress";
    public int?         TotalFields     { get; set; }
    public int          ConfirmedFields { get; set; }
    public int          FlaggedFields   { get; set; }
    public string?      IpAddress       { get; set; }
    public string?      UserAgent       { get; set; }
}
