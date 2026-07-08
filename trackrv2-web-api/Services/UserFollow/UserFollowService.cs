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

    public async Task<FollowResponse> FollowUserAsync(Guid followerId, Guid followingId)
    {
        if (followerId == followingId)
        {
            throw new InvalidOperationException($"Du kan ikke følge dig selv.");
        }

        var alreadyFollowing = await _ctx.UserFollows.AnyAsync(uf =>
            uf.FollowerId == followerId && uf.FollowingId == followingId
        );

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

        var userFollow = new UserFollow { FollowerId = followerId, FollowingId = followingId };

        await _ctx.UserFollows.AddAsync(userFollow);
        await _ctx.SaveChangesAsync();

        _cache.Remove($"{UserCachePrefix}{followerId}");
        _cache.Remove($"{UserCachePrefix}{followingId}");

        return new FollowResponse(followerUser.Username, followedUser.Username);
    }

    public async Task<List<UserFollowerResponse>> GetFollowersForUserAsync(
        Guid userId,
        string? userName,
        string? firstName,
        string? middleName,
        string? lastName,
        string? nationality,
        DateTime? followedAt
    )
    {
        var userExists = await _ctx.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new KeyNotFoundException($"Brugeren blev ikke fundet");
        }

        var followers = await GetManyFollowersAsync(
            userId,
            userName,
            firstName,
            middleName,
            lastName,
            nationality,
            followedAt
        );

        return followers
            .Select(follower => new UserFollowerResponse(
                follower.Follower.Id,
                follower.Follower.Username,
                follower.Follower.FirstName,
                follower.Follower.MiddleName,
                follower.Follower.LastName,
                follower.Follower.Nationality,
                follower.FollowedAt
            ))
            .ToList();
    }

    public async Task<List<UserFollowingResponse>> GetFollowingsForUserAsync(
        Guid userId,
        string? userName,
        string? firstName,
        string? middleName,
        string? lastName,
        string? nationality,
        DateTime? followingAt
    )
    {
        var userExists = await _ctx.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new KeyNotFoundException($"Brugeren blev ikke fundet");
        }

        var followings = await GetManyFollowingsAsync(
            userId,
            userName,
            firstName,
            middleName,
            lastName,
            nationality,
            followingAt
        );

        return followings
            .Select(following => new UserFollowingResponse(
                following.Following.Id,
                following.Following.Username,
                following.Following.FirstName,
                following.Following.MiddleName,
                following.Following.LastName,
                following.Following.Nationality,
                following.FollowedAt
            ))
            .ToList();
    }

    public async Task UnFollowUserAsync(Guid followerId, Guid followingId)
    {
        if (followerId == followingId)
        {
            throw new InvalidOperationException($"Du kan ikke stoppe med at følge dig selv.");
        }

        var followingUser = await _ctx.UserFollows.FirstOrDefaultAsync(uf =>
            uf.FollowerId == followerId && uf.FollowingId == followingId
        );

        if (followingUser == null)
        {
            throw new InvalidOperationException(
                $"Du følger ikke denne bruger og kan derfor ikke stoppe med at følge vedkommende."
            );
        }

        _ctx.UserFollows.Remove(followingUser);
        await _ctx.SaveChangesAsync();

        _cache.Remove($"{UserCachePrefix}{followerId}");
        _cache.Remove($"{UserCachePrefix}{followingId}");
    }

    private async Task<List<UserFollow>> GetManyFollowersAsync(
        Guid userId,
        string? userName,
        string? firstName,
        string? middleName,
        string? lastName,
        string? nationality,
        DateTime? followedAt
    )
    {
        var query = _ctx
            .UserFollows.AsNoTracking()
            .Include(uf => uf.Follower)
            .Where(u => u.FollowingId == userId);

        if (!string.IsNullOrWhiteSpace(userName))
        {
            query = query.Where(uf => EF.Functions.ILike(uf.Follower.Username, userName));
        }
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            query = query.Where(uf => EF.Functions.ILike(uf.Follower.FirstName, firstName));
        }
        if (!string.IsNullOrWhiteSpace(middleName))
        {
            query = query.Where(uf =>
                uf.Follower.MiddleName != null
                && EF.Functions.ILike(uf.Follower.MiddleName, middleName)
            );
        }
        if (!string.IsNullOrWhiteSpace(lastName))
        {
            query = query.Where(uf => EF.Functions.ILike(uf.Follower.LastName, lastName));
        }
        if (!string.IsNullOrWhiteSpace(nationality))
        {
            query = query.Where(uf =>
                uf.Follower.Nationality != null
                && EF.Functions.ILike(uf.Follower.Nationality, nationality)
            );
        }

        if (followedAt.HasValue)
        {
            DateTime startDate = followedAt.Value.Date;
            DateTime endDate = startDate.AddDays(1);
            query = query.Where(uf => uf.FollowedAt >= startDate && uf.FollowedAt < endDate);
        }

        return await query.ToListAsync();
    }

    private async Task<List<UserFollow>> GetManyFollowingsAsync(
        Guid userId,
        string? userName,
        string? firstName,
        string? middleName,
        string? lastName,
        string? nationality,
        DateTime? followingAt
    )
    {
        var query = _ctx
            .UserFollows.AsNoTracking()
            .Include(uf => uf.Following)
            .Where(u => u.FollowerId == userId);

        if (!string.IsNullOrWhiteSpace(userName))
        {
            query = query.Where(uf => EF.Functions.ILike(uf.Following.Username, userName));
        }
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            query = query.Where(uf => EF.Functions.ILike(uf.Following.FirstName, firstName));
        }
        if (!string.IsNullOrWhiteSpace(middleName))
        {
            query = query.Where(uf =>
                uf.Following.MiddleName != null
                && EF.Functions.ILike(uf.Following.MiddleName, middleName)
            );
        }
        if (!string.IsNullOrWhiteSpace(lastName))
        {
            query = query.Where(uf => EF.Functions.ILike(uf.Following.LastName, lastName));
        }
        if (!string.IsNullOrWhiteSpace(nationality))
        {
            query = query.Where(uf =>
                uf.Following.Nationality != null
                && EF.Functions.ILike(uf.Following.Nationality, nationality)
            );
        }

        if (followingAt.HasValue)
        {
            DateTime startDate = followingAt.Value.Date;
            DateTime endDate = startDate.AddDays(1);
            query = query.Where(uf => uf.FollowedAt >= startDate && uf.FollowedAt < endDate);
        }

        return await query.ToListAsync();
    }
}
