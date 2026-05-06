namespace POC.CustomerValidation.API.Models.Entites;

public class LibraryField
{
    public Guid     Id              { get; set; }
    public string   FieldKey        { get; set; } = default!;
    public string   FieldLabel      { get; set; } = default!;
    public string   FieldType       { get; set; } = default!;
    public string?  PlaceHolderText { get; set; }
    public string?  HelpText        { get; set; }
    public bool     IsRequired      { get; set; }
    public int      DisplayOrder    { get; set; }
    public decimal? MinValue        { get; set; }
    public decimal? MaxValue        { get; set; }
    public int?     MinLength       { get; set; }
    public int?     MaxLength       { get; set; }
    public string?  RegExPattern    { get; set; }
    public string?  DisplayFormat   { get; set; }
    public bool     IsActive        { get; set; } = true;
    public DateTime CreatedDt       { get; set; }
    public DateTime ModifiedDt      { get; set; }
}
