using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using trackrv2_efc;
using trackrv2_efc.Entities;
using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.TrackerEntryService;

public class TrackerEntryService : ITrackerEntryService
{
    private readonly TrackrContext _ctx;
    private readonly IMemoryCache _cache;
    private const string TrackerEntryCachePrefix = "tracker-entry_";
    private const string TrackerCachePrefix = "tracker_";
    private const string UserCachePrefix = "user_";

    public TrackerEntryService(TrackrContext ctx, IMemoryCache cache)
    {
        _ctx = ctx;
        _cache = cache;
    }

    public async Task<TrackerEntryResponse> CreateTrackerEntryAsync(Guid trackerId, Guid userId, TrackerEntryRequest request)
    {
        var trackerExists = await _ctx.Trackers
           .AnyAsync(t => t.Id == trackerId && t.UserId == userId);

        if (!trackerExists)
        {
            throw new KeyNotFoundException($"Tracker med id'et '{trackerId}' for brugeren med id'et '{userId}' blev ikke fundet.");
        }

        var trackerEntry = new TrackerEntry
        {
            TrackerId = trackerId,
            Values = request.Values.Select(ev => new EntryValue
            {
                Value = ev.Value,
                FieldDefinitionId = ev.FieldDefinitionId,
            }).ToList()
        };

        var addedTrackerEntry = await _ctx.TrackerEntries.AddAsync(trackerEntry);
        await _ctx.SaveChangesAsync();
        var addedTrackerEntryEntity = addedTrackerEntry.Entity;

        var savedTrackerEntryWithFields = await _ctx.TrackerEntries
        .AsNoTracking()
        .Include(te => te.Values)
        .ThenInclude(ev => ev.FieldDefinition)
        .FirstAsync(te => te.Id == trackerEntry.Id);

        string userCacheKey = $"{UserCachePrefix}{userId}";
        _cache.Remove(userCacheKey);
        string trackerCacheKey = $"{TrackerCachePrefix}{trackerId}";
        _cache.Remove(trackerCacheKey);
        string trackerEntryCacheKey = $"{TrackerEntryCachePrefix}{trackerId}";
        _cache.Remove(trackerEntryCacheKey);

        return new TrackerEntryResponse(
            savedTrackerEntryWithFields.Id,
            savedTrackerEntryWithFields.TrackerId,
            savedTrackerEntryWithFields.Values
            .Select(ev => new EntryValueResponse(
                ev.Id,
                ev.FieldDefinitionId,
                ev.FieldDefinition.Label,
                ev.FieldDefinition.Type,
                ev.Value,
                ev.CreatedAt,
                ev.LastUpdated
            )).ToList(),
            savedTrackerEntryWithFields.CreatedAt,
            savedTrackerEntryWithFields.LastUpdated);
    }

    public async Task DeleteTrackerEntryAsync(Guid trackerEntryId, Guid userId)
    {
        var existingTrackerEntryForUser = await _ctx.TrackerEntries
               .FirstOrDefaultAsync(te => te.Id == trackerEntryId && te.Tracker.UserId == userId);

        if (existingTrackerEntryForUser == null)
        {
            var existingUser = await _ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            throw new KeyNotFoundException($"Tracker Entry'en med id'et '{trackerEntryId}' og brugeren '{existingUser?.Username}' blev ikke fundet");
        }

        _ctx.TrackerEntries.Remove(existingTrackerEntryForUser);
        await _ctx.SaveChangesAsync();

        _cache.Remove($"{TrackerEntryCachePrefix}{existingTrackerEntryForUser.TrackerId}");
        string trackerCacheKey = $"{TrackerCachePrefix}{existingTrackerEntryForUser.TrackerId}";
        _cache.Remove(trackerCacheKey);
        string userCacheKey = $"{UserCachePrefix}{userId}";
        _cache.Remove(userCacheKey);
    }

    public async Task<List<TrackerEntryResponse>> GetTrackerEntriesForTrackerAsync(Guid trackerId, Guid userId, DateTime? fromCreatedAtDate, DateTime? toCreatedAtDate)
    {
        var trackerExists = await _ctx.Trackers.AnyAsync(t => t.Id == trackerId && t.UserId == userId);
        if (!trackerExists)
        {
            var existingUser = await _ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            throw new KeyNotFoundException($"Brugeren '{existingUser?.Username}' og trackeren med id'et '{trackerId}' blev ikke fundet eller matcher ikke.");
        }

        string cacheKey = $"{TrackerEntryCachePrefix}{trackerId}";

        return (await _cache.GetOrCreateAsync(cacheKey, async entry =>
         {
             entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

             var query = _ctx.TrackerEntries
            .AsNoTracking()
            .Include(t => t.Tracker)
            .Where(te => te.TrackerId == trackerId && te.Tracker.UserId == userId)
            .Include(te => te.Values)
            .ThenInclude(ve => ve.FieldDefinition)
            .AsQueryable();

             if (fromCreatedAtDate.HasValue)
             {
                 query = query.Where(te =>
                     te.CreatedAt >= fromCreatedAtDate.Value);
             }

             if (toCreatedAtDate.HasValue)
             {

                 query = query.Where(te =>
                     te.CreatedAt <= toCreatedAtDate.Value);
             }

             query = query.OrderByDescending(te => te.CreatedAt);

             var trackerEntries = await query.ToListAsync();

             return trackerEntries.Select(te => new TrackerEntryResponse(
                te.Id,
                te.TrackerId,
                te.Values.Select(ev => new EntryValueResponse(
                    ev.Id,
                    ev.FieldDefinitionId,
                    ev.FieldDefinition.Label,
                    ev.FieldDefinition.Type,
                    ev.Value,
                    ev.CreatedAt,
                    ev.LastUpdated
            )).ToList(),
            te.CreatedAt,
            te.LastUpdated
        )).ToList();
         }))!;
    }

    public async Task UpdateTrackerEntryAsync(Guid trackerEntryId, Guid userId, TrackerEntryRequest request)
    {
        var existingTrackerEntry = await _ctx.TrackerEntries
            .Include(t => t.Values)
            .FirstOrDefaultAsync(t => t.Id == trackerEntryId && t.Tracker.UserId == userId);

        if (existingTrackerEntry == null)
        {
            var existingUser = await _ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            throw new KeyNotFoundException($"Tracker Entry'en med id'et '{trackerEntryId}' og brugeren '{existingUser?.Username}' blev ikke fundet");
        }

        bool isModified = false;

        foreach (var ev in request.Values)
        {
            var existingValue = existingTrackerEntry.Values
                .FirstOrDefault(v => v.FieldDefinitionId == ev.FieldDefinitionId);

            if (existingValue != null)
            {
                if (existingValue.Value != ev.Value)
                {
                    existingValue.Value = ev.Value;
                    isModified = true;
                }
            }
            else
            {
                existingTrackerEntry.Values.Add(new EntryValue
                {
                    Value = ev.Value,
                    FieldDefinitionId = ev.FieldDefinitionId,
                });
                isModified = true;
            }
        }
        if (isModified)
        {
            _ctx.Entry(existingTrackerEntry).State = EntityState.Modified;
        }

        await _ctx.SaveChangesAsync();

        _cache.Remove($"{TrackerEntryCachePrefix}{existingTrackerEntry.TrackerId}");
        string trackerCacheKey = $"{TrackerCachePrefix}{existingTrackerEntry.TrackerId}";
        _cache.Remove(trackerCacheKey);
        string userCacheKey = $"{UserCachePrefix}{userId}";
        _cache.Remove(userCacheKey);
    }

}
