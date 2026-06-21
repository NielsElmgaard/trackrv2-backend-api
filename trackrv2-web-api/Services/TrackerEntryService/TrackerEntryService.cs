using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.TrackerEntryService;

public class TrackerEntryService : ITrackerEntryService
{
    public Task CreateEntryAsync(Guid trackerId, TrackerEntryRequest trackerEntryRequest)
    {
        throw new NotImplementedException();
    }

    public Task DeleteEntryAsync(Guid entryId)
    {
        throw new NotImplementedException();
    }

    public Task<List<TrackerEntryResponse>> GetEntriesForTrackerAsync(Guid trackerId, DateTime? fromCreatedAtDate, DateTime? toCreatedAtDate)
    {
        throw new NotImplementedException();
    }

    public Task UpdateEntryAsync(Guid entryId, TrackerEntryRequest request)
    {
        throw new NotImplementedException();
    }
}