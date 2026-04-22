namespace POC.CustomerValidation.API.Models.Entites;

// For group selection fields, like dropdown boxes.
public class FieldOption
{
    public Guid     OptionId            { get; set; }
    public Guid     FieldDefinitionId   { get; set; }
    public string   OptionKey           { get; set; } = default!;
    public string   OptionLabel         { get; set; } = default!;
    public int      DisplayOrder        { get; set; }
    public bool     IsActive            { get; set; }
}

