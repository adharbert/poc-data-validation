namespace POC.CustomerValidation.API.Models.Entites;

// Field answer history.
public class FieldValueHistory
{
    public Guid         HistoryId           { get; set; }
    public Guid         FieldValueId        { get; set; }
    public Guid         CustomerId          { get; set; }
    public Guid         FieldDefinitionId   { get; set; }
    public string?      ValueText           { get; set; }
    public decimal?     ValueNumber         { get; set; }
    public DateOnly?    ValueDate           { get; set; }
    public DateTime?    ValueDateTime       { get; set; }
    public bool?        ValueBoolean        { get; set; }
    public string?      ChangedBy           { get; set; }
    public DateTime     ChangeAt            { get; set; }
    public string?      ChangeReason        { get; set; }
}
