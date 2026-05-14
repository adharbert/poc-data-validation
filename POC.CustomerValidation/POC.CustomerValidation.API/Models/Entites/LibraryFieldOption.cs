namespace POC.CustomerValidation.API.Models.Entites;

public class LibraryFieldOption
{
    public Guid     Id              { get; set; }
    public Guid     LibraryFieldId  { get; set; }
    public string   OptionKey       { get; set; } = default!;
    public string   OptionLabel     { get; set; } = default!;
    public int      DisplayOrder    { get; set; }
    public bool     IsActive        { get; set; } = true;
}
