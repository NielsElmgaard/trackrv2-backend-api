using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.TrackerEntryService;

public interface ITrackerEntryService
{
    Task CreateEntryAsync(Guid trackerId, TrackerEntryRequest trackerEntryRequest);
    Task<List<TrackerEntryResponse>> GetEntriesForTrackerAsync(Guid trackerId, DateTime? fromCreatedAtDate, DateTime? toCreatedAtDate);
    Task DeleteEntryAsync(Guid entryId);
    Task UpdateEntryAsync(Guid entryId, TrackerEntryRequest request);
}