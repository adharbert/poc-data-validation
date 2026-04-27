namespace POC.CustomerValidation.API.Models.Entites;

public class Contract
{
    public Guid         ContractId      { get; set; }
    public Guid         OrganizationId  { get; set; }
    public string       ContractName    { get; set; } = default!;
    public string?      ContractNumber  { get; set; }
    public DateOnly     StartDate       { get; set; }
    public DateOnly?    EndDate         { get; set; }
    public bool         IsActive        { get; set; } = true;
    public string?      Notes           { get; set; }
    public DateTime     CreatedDt       { get; set; }
    public string       CreatedBy       { get; set; } = default!;
    public DateTime?    ModifiedDt      { get; set; }
    public string?      ModifiedBy      { get; set; }
}
