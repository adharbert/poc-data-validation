namespace POC.CustomerValidation.API.Models.Entites;

public class LibrarySection
{
    public Guid     Id              { get; set; }
    public string   SectionName     { get; set; } = default!;
    public string?  Description     { get; set; }
    public int      DisplayOrder    { get; set; }
    public bool     IsActive        { get; set; } = true;
    public DateTime CreatedDt       { get; set; }
    public DateTime ModifiedDt      { get; set; }
}
