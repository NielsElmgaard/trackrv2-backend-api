namespace trackrv2_efc.Entities;

public class UserFollow
{
    public User Follower { get; set; } = null!; // The user that follows someone
    public Guid FollowerId { get; set; }

    public User Following { get; set; } = null!; // The user that is being followed
    public Guid FollowingId { get; set; }
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

}