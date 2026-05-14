namespace POC.CustomerValidation.API.Models.Entites;

public class Customer
{
    public Guid     CustomerId      { get; set; }
    public Guid     OrganizationId  { get; set; }
    public string   FirstName       { get; set; } = default!;
    public string   LastName        { get; set; } = default!;
    public string?   MiddleName      { get; set; }
    public string?   MaidenName      { get; set; }
    public DateOnly? DateOfBirth     { get; set; }
    public string    CustomerCode    { get; set; } = default!;
    public string?   OriginalId      { get; set; }
    public string?   Email           { get; set; }
    public string?   Phone           { get; set; }
    public bool?    IsActive        { get; set; } = true;
    public DateTime CreatedDate     { get; set; }
    public DateTime ModifiedDate    { get; set; }
}
