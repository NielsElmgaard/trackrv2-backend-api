namespace trackrv2_shared.DTOs;

public record TrackerOverviewResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime LastUpdated
);
