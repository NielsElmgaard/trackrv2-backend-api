using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.TrackerEntryService;

public interface ITrackerEntryService
{
    Task<TrackerEntryResponse> CreateTrackerEntryAsync(
        Guid trackerId,
        Guid userId,
        TrackerEntryRequest request
    );
    Task<List<TrackerEntryResponse>> GetTrackerEntriesForTrackerAsync(
        Guid trackerId,
        Guid userId,
        DateTime? fromCreatedAtDate,
        DateTime? toCreatedAtDate
    );
    Task DeleteTrackerEntryAsync(Guid trackerEntryId, Guid userId);
    Task UpdateTrackerEntryAsync(Guid trackerEntryId, Guid userId, TrackerEntryRequest request);
}
