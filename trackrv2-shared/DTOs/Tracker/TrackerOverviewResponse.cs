namespace trackrv2_shared.DTOs;

public record TrackerOverviewResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime LastUpdated
);
