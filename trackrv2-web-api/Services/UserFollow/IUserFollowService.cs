
using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.IUserFollowService;

public interface IUserFollowService
{
    Task<FollowResponse> FollowUser(Guid followerId, Guid followingId);

    Task<List<UserFollowerResponse>> GetFollowersForUser(Guid userId,
    string? userName,
    string? firstName,
    string? middleName,
    string? lastName,
    string? nationality,
    DateTime? followedAt);

    Task UnFollowUser(Guid followerId, Guid followingId);
}