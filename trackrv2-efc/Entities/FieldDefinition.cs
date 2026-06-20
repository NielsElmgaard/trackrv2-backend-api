namespace trackrv2_efc.Entities;

public class FieldDefinition : BaseEntity
{
    public required string Label { get; set; }
    public FieldType Type { get; set; }
    
    public Guid TrackerId { get; set; }
    public Tracker Tracker { get; set; } = null!;
    
    public List<EntryValue> Values { get; set; } = new();
}