namespace POC.CustomerValidation.API.Models.Entites;

public class MarketingProject
{
    public int          ProjectId           { get; set; }
    public Guid         OrganizationId      { get; set; }
    public Guid?        ContractId          { get; set; }
    public string       ProjectName         { get; set; } = default!;
    public DateOnly     MarketingStartDate  { get; set; }
    public DateOnly?    MarketingEndDate    { get; set; }
    public bool         IsActive            { get; set; } = true;
    public string?      Notes               { get; set; }
    public DateTime     CreatedDt           { get; set; }
    public string       CreatedBy           { get; set; } = default!;
    public DateTime?    ModifiedDt          { get; set; }
    public string?      ModifiedBy          { get; set; }
}
