namespace POC.CustomerValidation.API.Models.Entites;

public class Organization
{
    public Guid         OrganizationId      { get; set; }
    public string       OrganizationName    { get; set; } = default!;
    public string?      FilingName          { get; set; }
    public string?      MarketingName       { get; set; }
    public string?      Abbreviation        { get; set; }
    public string       OrganizationCode    { get; set; } = default!;
    public string?      Website             { get; set; }
    public string?      Phone               { get; set; }
    public string?      CompanyEmail        { get; set; }
    public bool?        IsActive            { get; set; } = true;
    public DateTime?    CreatedDate         { get; set; }
    public string       CreatedBy           { get; set; } = default!;
    public DateTime?    ModifiedDate        { get; set; }
    public string?      ModifiedBy          { get; set; }

}
