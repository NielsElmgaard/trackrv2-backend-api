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

    public async Task<TrackerDetailedResponse> CreateTrackerAsync(
        Guid userId,
        TrackerRequest request
    )
    {
        var trackerNameForUserExists = await _ctx.Trackers.AnyAsync(t =>
            t.UserId == userId && t.Name == request.Name
        );

        if (trackerNameForUserExists)
        {
            throw new InvalidOperationException(
                $"Du har allerede en tracker med navnet '{request.Name}'."
            );
        }

        var tracker = new Tracker
        {
            Name = request.Name,
            UserId = userId,
            Fields = request
                .Fields.Select(f => new FieldDefinition { Label = f.Label, Type = f.Type })
                .ToList(),
        };

        var addedTracker = await _ctx.Trackers.AddAsync(tracker);
        await _ctx.SaveChangesAsync();
        var addedTrackerEntity = addedTracker.Entity;

        _cache.Remove($"{UserCachePrefix}{userId}");

        return new TrackerDetailedResponse(
            addedTrackerEntity.Id,
            addedTrackerEntity.Name,
            addedTrackerEntity.UserId,
            addedTrackerEntity.CreatedAt,
            addedTrackerEntity.LastUpdated,
            addedTrackerEntity
                .Fields.Select(f => new FieldDefinitionResponse(
                    f.Id,
                    f.Label,
                    f.Type,
                    f.CreatedAt,
                    f.LastUpdated
                ))
                .ToList()
        );
    }

    public async Task DeleteTrackerAsync(Guid trackerId, Guid userId)
    {
        var existingTrackerForUser = await _ctx.Trackers.FirstOrDefaultAsync(t =>
            t.Id == trackerId && t.UserId == userId
        );

        if (existingTrackerForUser == null)
        {
            throw new KeyNotFoundException(
                $"Trackeren med id'et '{trackerId}' blev ikke fundet for denne bruger."
            );
        }

        _ctx.Trackers.Remove(existingTrackerForUser);
        await _ctx.SaveChangesAsync();

        _cache.Remove($"{TrackerCachePrefix}{userId}_{trackerId}");
        _cache.Remove($"{UserCachePrefix}{userId}");
    }

    public async Task<TrackerDetailedResponse> GetTrackerByIdAsync(Guid trackerId, Guid userId)
    {
        string cacheKey = $"{TrackerCachePrefix}{userId}_{trackerId}";

        return (
            await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                    var tracker = await _ctx
                        .Trackers.AsNoTracking()
                        .Include(t => t.Fields)
                        .FirstOrDefaultAsync(t => t.Id == trackerId && t.UserId == userId);

                    if (tracker == null)
                    {
                        throw new KeyNotFoundException(
                            $"Trackeren med id'et '{trackerId}' blev ikke fundet for denne bruger."
                        );
                    }

                    return new TrackerDetailedResponse(
                        tracker.Id,
                        tracker.Name,
                        tracker.UserId,
                        tracker.CreatedAt,
                        tracker.LastUpdated,
                        tracker
                            .Fields.Select(f => new FieldDefinitionResponse(
                                f.Id,
                                f.Label,
                                f.Type,
                                f.CreatedAt,
                                f.LastUpdated
                            ))
                            .ToList()
                    );
                }
            )
        )!;
    }

    public async Task UpdateTrackerAsync(Guid trackerId, Guid userId, TrackerRequest request)
    {
        var existingTrackerForUser = await _ctx
            .Trackers.Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == trackerId && t.UserId == userId);

        if (existingTrackerForUser == null)
        {
            throw new KeyNotFoundException($"Tracker blev ikke fundet.");
        }

        if (existingTrackerForUser.Name != request.Name)
        {
            var nameExists = await _ctx.Trackers.AnyAsync(t =>
                t.UserId == userId && t.Name == request.Name
            );
            if (nameExists)
                throw new InvalidOperationException(
                    $"Du har allerede en tracker med navnet '{request.Name}'."
                );

            existingTrackerForUser.Name = request.Name;
        }

        // All existing fields
        var requestFieldIds = request
            .Fields.Where(rf => rf.Id != Guid.Empty)
            .Select(rf => rf.Id)
            .ToList();

        // Remove fields that are not in the request anymore based on ID
        var fieldsToRemove = existingTrackerForUser
            .Fields.Where(f => !requestFieldIds.Contains(f.Id))
            .ToList();
        foreach (var fieldToRemove in fieldsToRemove)
        {
            _ctx.FieldDefinitions.Remove(fieldToRemove);
        }

        // Add new fields or update existing ones
        foreach (var fieldRequest in request.Fields)
        {
            if (fieldRequest.Id == Guid.Empty)
            {
                existingTrackerForUser.Fields.Add(
                    new FieldDefinition { Label = fieldRequest.Label, Type = fieldRequest.Type }
                );
            }
            else
            {
                var existingField = existingTrackerForUser.Fields.FirstOrDefault(f =>
                    f.Id == fieldRequest.Id
                );

                if (existingField != null)
                {
                    existingField.Label = fieldRequest.Label;

                    if (existingField.Type != fieldRequest.Type)
                    {
                        var hasExistingValues = await _ctx.EntryValues.AnyAsync(v =>
                            v.FieldDefinitionId == existingField.Id
                        );

                        if (hasExistingValues)
                        {
                            throw new InvalidOperationException(
                                $"Du har allerede værdier i feltet og kan derfor ikke ændre datatypen for feltet '{existingField.Label}'."
                            );
                        }

                        existingField.Type = fieldRequest.Type;
                    }
                }
            }
        }

        await _ctx.SaveChangesAsync();

        _cache.Remove($"{TrackerCachePrefix}{userId}_{trackerId}");
        _cache.Remove($"{UserCachePrefix}{userId}");
    }

    public async Task<List<TrackerOverviewResponse>> GetTrackersByUserAsync(
        Guid userId,
        string? name,
        DateTime? createdAt,
        DateTime? lastUpdated
    )
    {
        var userExists = await _ctx.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new KeyNotFoundException($"brugeren med id'et '{userId}' blev ikke fundet");
        }
        var trackers = await GetManyTrackers(userId, name, createdAt, lastUpdated);

        return trackers
            .Select(tracker =>
            {
                return new TrackerOverviewResponse(
                    tracker.Id,
                    tracker.Name,
                    tracker.CreatedAt,
                    tracker.LastUpdated
                );
            })
            .ToList();
    }

    private async Task<List<Tracker>> GetManyTrackers(
        Guid userId,
        string? name,
        DateTime? createdAt,
        DateTime? lastUpdated
    )
    {
        var query = _ctx.Trackers.AsNoTracking().Where(t => t.UserId == userId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(t => EF.Functions.ILike(t.Name, name));
        }

        if (createdAt.HasValue)
        {
            DateTime startDate = createdAt.Value.Date;
            DateTime endDate = startDate.AddDays(1);
            query = query.Where(t => t.CreatedAt >= startDate && t.CreatedAt < endDate);
        }

        if (lastUpdated.HasValue)
        {
            DateTime startDate = lastUpdated.Value.Date;
            DateTime endDate = startDate.AddDays(1);
            query = query.Where(t => t.LastUpdated >= startDate && t.LastUpdated < endDate);
        }

        return await query.ToListAsync();
    }
}
