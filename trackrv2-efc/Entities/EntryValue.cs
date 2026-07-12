namespace trackrv2_efc.Entities;

public class EntryValue : BaseEntity
{
    public required string Value { get; set; }

    public Guid FieldDefinitionId { get; set; }
    public FieldDefinition FieldDefinition { get; set; } = null!;

    public Guid TrackerEntryId { get; set; }
    public TrackerEntry TrackerEntry { get; set; } = null!;
}
