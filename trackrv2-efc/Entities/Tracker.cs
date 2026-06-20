namespace trackrv2_efc.Entities;

public class Tracker : BaseEntity
{
    public required string Name { get; set; }
    public List<FieldDefinition> Fields { get; set; } = new();
    public List<TrackerEntry> Entries { get; set; } = new();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}