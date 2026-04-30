namespace POC.CustomerValidation.API.Models.Entites;

public class FieldSection
{
    public Guid     SectionId  { get; set; }
    public Guid     OrganizationId  { get; set; }
    public string   SectionName     { get; set; } = default!;
    public int      DisplayOrder    { get; set; }
    public bool     IsActive        { get; set; }
}
