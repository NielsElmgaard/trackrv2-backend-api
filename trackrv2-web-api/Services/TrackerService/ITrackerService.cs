using trackrv2_shared.DTOs;

namespace trackrv2_web_api.Services.TrackerService;

public interface ITrackerService
{
    Task<TrackerDetailedResponse> CreateTrackerAsync(Guid userId, TrackerRequest request);
    Task<TrackerDetailedResponse> GetTrackerByIdAsync(Guid trackerId, Guid userId);
    Task DeleteTrackerAsync(Guid trackerId, Guid userId);
    Task UpdateTrackerNameAsync(Guid trackerId, Guid userId, string newName);
}