namespace POC.CustomerValidation.API.Models.Entites;

public class CustomerPhone
{
    public Guid     PhoneId         { get; set; }
    public Guid     CustomerId      { get; set; }
    public string   PhoneNumber     { get; set; } = default!;
    public string   PhoneType       { get; set; } = "mobile";
    public bool     IsPrimary       { get; set; }
    public bool     IsActive        { get; set; } = true;
    public DateTime CreatedUtcDt    { get; set; }
    public DateTime ModifiedUtcDt   { get; set; }
}
