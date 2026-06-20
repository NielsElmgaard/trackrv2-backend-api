namespace trackrv2_efc.Entities;
public class TrackerEntry : BaseEntity
{
    public Guid TrackerId { get; set; }
    public Tracker Tracker { get; set; } = null!;

    public List<EntryValue> Values { get; set; } = new();
    
}