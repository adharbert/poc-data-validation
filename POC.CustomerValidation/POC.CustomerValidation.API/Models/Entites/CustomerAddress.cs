namespace POC.CustomerValidation.API.Models.Entites;

public class CustomerAddress
{
    public Guid     AddressId           { get; set; }
    public Guid     CustomerId          { get; set; }
    public string   AddressLine1        { get; set; } = default!;
    public string?  AddressLine2        { get; set; }
    public string   City                { get; set; } = default!;
    public string   State               { get; set; } = default!;
    public string   PostalCode          { get; set; } = default!;
    public string   Country             { get; set; } = "US";
    public string   AddressType         { get; set; } = "primary";
    public double?  Latitude            { get; set; }
    public double?  Longitude           { get; set; }
    public bool     MelissaValidated    { get; set; }
    public bool     CustomerConfirmed   { get; set; }
    public bool     IsCurrent           { get; set; }
    public DateTime CreatedUtcDt        { get; set; }
    public DateTime ModifiedUtcDt       { get; set; }
}
