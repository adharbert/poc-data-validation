namespace POC.CustomerValidation.API.Models.Entites;

public class CustomerEmail
{
    public Guid     EmailId         { get; set; }
    public Guid     CustomerId      { get; set; }
    public string   EmailAddress    { get; set; } = default!;
    public string   EmailType       { get; set; } = "personal";
    public bool     IsPrimary       { get; set; }
    public bool     IsActive        { get; set; } = true;
    public DateTime CreatedUtcDt    { get; set; }
    public DateTime ModifiedUtcDt   { get; set; }
}
