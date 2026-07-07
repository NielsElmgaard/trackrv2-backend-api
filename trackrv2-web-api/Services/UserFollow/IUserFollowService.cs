using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.IUserFollowService;

public interface IUserFollowService
{
    Task<FollowResponse> FollowUserAsync(Guid followerId, Guid followingId);

    /// <summary>
    /// Get the users that is following the requested user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="userName"></param>
    /// <param name="firstName"></param>
    /// <param name="middleName"></param>
    /// <param name="lastName"></param>
    /// <param name="nationality"></param>
    /// <param name="followedAt"></param>
    /// <returns></returns>
    Task<List<UserFollowerResponse>> GetFollowersForUserAsync(
        Guid userId,
        string? userName,
        string? firstName,
        string? middleName,
        string? lastName,
        string? nationality,
        DateTime? followedAt
    );

    /// <summary>
    /// Get the the users that the requested user is following
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="userName"></param>
    /// <param name="firstName"></param>
    /// <param name="middleName"></param>
    /// <param name="lastName"></param>
    /// <param name="nationality"></param>
    /// <param name="followingAt"></param>
    /// <returns></returns>
    Task<List<UserFollowingResponse>> GetFollowingsForUserAsync(
        Guid userId,
        string? userName,
        string? firstName,
        string? middleName,
        string? lastName,
        string? nationality,
        DateTime? followingAt
    );

    /// <summary>
    /// Delete a UserFollow from the the requested user's following list
    /// </summary>
    /// <param name="followerId"></param>
    /// <param name="followingId"></param>
    /// <returns></returns>
    Task UnFollowUserAsync(Guid followerId, Guid followingId);
}
