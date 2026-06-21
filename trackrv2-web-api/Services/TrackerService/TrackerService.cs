using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using trackrv2_efc;
using trackrv2_efc.Entities;
using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.TrackerService;

public class TrackerService : ITrackerService
{
    private readonly TrackrContext _ctx;
    private readonly IMemoryCache _cache;
    private const string TrackerCachePrefix = "tracker_";
    private const string UserCachePrefix = "user_";



    public TrackerService(TrackrContext ctx, IMemoryCache cache)
    {
        _ctx = ctx;
        _cache = cache;
    }


    public async Task<TrackerDetailedResponse> CreateTrackerAsync(Guid userId, TrackerRequest request)
    {
        var trackerNameForUserExists = await _ctx.Trackers
            .AnyAsync(t => t.UserId == userId && t.Name == request.Name);

        if (trackerNameForUserExists)
        {
            var existingUser = await _ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            throw new InvalidOperationException($"Bruger '{existingUser?.Username}' har allerede en tracker med navnet '{request.Name}'.");
        }

        var tracker = new Tracker
        {
            Name = request.Name,
            UserId = userId,
            Fields = request.Fields.Select(f => new FieldDefinition
            {
                Label = f.Label,
                Type = f.Type
            }).ToList()
        };

        var addedTracker = await _ctx.Trackers.AddAsync(tracker);
        await _ctx.SaveChangesAsync();
        var addedTrackerEntity = addedTracker.Entity;

        string userCacheKey = $"{UserCachePrefix}{userId}";
        _cache.Remove(userCacheKey);

        return new TrackerDetailedResponse(addedTrackerEntity.Id, addedTrackerEntity.Name, addedTrackerEntity.UserId, addedTrackerEntity.CreatedAt, addedTrackerEntity.LastUpdated, addedTrackerEntity.Fields.Select(f => new FieldDefinitionResponse(
            f.Id,
            f.Label,
            f.Type,
            f.CreatedAt,
            f.LastUpdated
        )).ToList());
    }

    public async Task DeleteTrackerAsync(Guid trackerId, Guid userId)
    {
        var existingTrackerForUser = await _ctx.Trackers
       .FirstOrDefaultAsync(t => t.Id == trackerId && t.UserId == userId);

        if (existingTrackerForUser == null)
        {
            var existingUser = await _ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            throw new KeyNotFoundException($"Trackeren med id'et '{trackerId}' og brugeren '{existingUser?.Username}' blev ikke fundet");
        }

        _ctx.Trackers.Remove(existingTrackerForUser);
        await _ctx.SaveChangesAsync();

        string trackerCacheKey = $"{TrackerCachePrefix}{trackerId}";
        _cache.Remove(trackerCacheKey);
        string userCacheKey = $"{UserCachePrefix}{userId}";
        _cache.Remove(userCacheKey);
    }

    public async Task<TrackerDetailedResponse> GetTrackerByIdAsync(Guid trackerId, Guid userId)
    {
        string cacheKey = $"{TrackerCachePrefix}{trackerId}";

        return (await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            var tracker = await _ctx.Trackers
                .AsNoTracking()
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == trackerId && t.UserId == userId);

            if (tracker == null)
            {
                var existingUser = await _ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
                throw new KeyNotFoundException($"Brugeren '{existingUser?.Username}' og trackeren med id'et '{trackerId}' blev ikke fundet eller macther ikke.");

            }

            return new TrackerDetailedResponse(tracker.Id,
                tracker.Name, tracker.UserId,
                 tracker.CreatedAt,
                tracker.LastUpdated,
                tracker.Fields.Select(f => new FieldDefinitionResponse(f.Id, f.Label, f.Type, f.CreatedAt, f.LastUpdated)).ToList());
        }))!;
    }

    public async Task UpdateTrackerNameAsync(Guid trackerId, Guid userId, string newName)
    {
        var existingTrackerForUser = await _ctx.Trackers
       .FirstOrDefaultAsync(t => t.Id == trackerId && t.UserId == userId);

        if (existingTrackerForUser == null)
        {
            var existingUser = await _ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            throw new KeyNotFoundException($"Trackeren med id'et '{trackerId}' og brugeren '{existingUser?.Username}' blev ikke fundet");
        }

        // If updating tracker name make sure it is not taken already by same user

        if (existingTrackerForUser.Name != newName)
        {
            var trackerNameForUserExists = await _ctx.Trackers
            .AnyAsync(t => t.UserId == userId && t.Name == newName);

            if (trackerNameForUserExists)
            {
                var existingUser = await _ctx.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);
                throw new InvalidOperationException($"Bruger '{existingUser?.Username}' har allerede en tracker med navnet '{newName}'.");
            }

        }

        existingTrackerForUser.Name = newName;

        await _ctx.SaveChangesAsync();

        string trackerCacheKey = $"{TrackerCachePrefix}{trackerId}";
        _cache.Remove(trackerCacheKey);
        string userCacheKey = $"{UserCachePrefix}{userId}";
        _cache.Remove(userCacheKey);
    }
}