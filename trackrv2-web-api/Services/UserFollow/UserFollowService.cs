using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using trackrv2_efc;
using trackrv2_efc.Entities;
using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.IUserFollowService;

public class UserFollowService(TrackrContext ctx, IMemoryCache cache) : IUserFollowService
{
    private readonly TrackrContext _ctx = ctx;
    private readonly IMemoryCache _cache = cache;

    private const string UserCachePrefix = "user_";
    public async Task<FollowResponse> FollowUser(Guid followerId, Guid followingId)
    {

        if (followerId == followingId)
        {
            throw new InvalidOperationException($"Du kan ikke følge dig selv.");
        }

        var alreadyFollowing = await _ctx.UserFollows.AnyAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId);


        if (alreadyFollowing)
        {
            throw new InvalidOperationException($"Du følger allerede denne bruger.");
        }

        var followerUser = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == followerId);
        var followedUser = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == followingId);

        if (followerUser == null || followedUser == null)
        {
            throw new KeyNotFoundException("En eller begge brugere findes ikke.");
        }

        var userFollow = new UserFollow
        {
            FollowerId = followerId,
            FollowingId = followingId
        };

        await _ctx.UserFollows.AddAsync(userFollow);
        await _ctx.SaveChangesAsync();

        _cache.Remove($"{UserCachePrefix}{followerId}");
        _cache.Remove($"{UserCachePrefix}{followingId}");

        return new FollowResponse(followerUser.Username,
        followedUser.Username);
    }


    public async Task<List<UserFollowerResponse>> GetFollowersForUser(Guid userId,
    string? userName,
    string? firstName,
    string? middleName,
    string? lastName,
    string? nationality,
    DateTime? followedAt)
    {
        var userExists = await _ctx.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new KeyNotFoundException($"Brugeren blev ikke fundet");
        }

        var followers = await GetManyFollowers(userId, userName, firstName, middleName, lastName, nationality, followedAt);

        throw new NotImplementedException();
    }

    public Task UnFollowUser(Guid followerId, Guid followingId)
    {
        throw new NotImplementedException();
    }

    private async Task<List<trackrv2_efc.Entities.User>> GetManyFollowers(Guid userId,
    string? userName,
    string? firstName,
    string? middleName,
    string? lastName,
    string? nationality,
    DateTime? followedAt)
    {
        var query = _ctx.UserFollows.AsNoTracking().Where(u => u.FollowerId == userId);
        throw new NotImplementedException();

    }
}