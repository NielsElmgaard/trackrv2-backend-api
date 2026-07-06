namespace trackrv2_shared.DTOs;

public record FollowResponse(
    string FollowerUsername, // The user that just followed someone
    string FollowingUsername // The user that was being followed
);