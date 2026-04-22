namespace POC.CustomerValidation.API.Models.Entites;

// Field values for the answers Customers give for the data validation.
public class FieldValue
{
    public Guid         FieldValueId        { get; set; }
    public Guid         CustomerId          { get; set; }
    public Guid         FieldDefinitionId   { get; set; }
    public string?      ValueText           { get; set; }
    public decimal?     ValueNumber         { get; set; }
    public DateOnly?    ValueDate           { get; set; }
    public DateTime?    ValueDatetime       { get; set; }
    public bool?        ValueBoolean        { get; set; }
    public DateTime?    ConfirmedAt         { get; set; }
    public string?      ConfirmedBy         { get; set; }
    public DateTime?    FlaggedAt           { get; set; }
    public string?      FlagNote            { get; set; }
    public DateTime     CreatedDt           { get; set; }
    public DateTime     ModifiedDt          { get; set; }
}
