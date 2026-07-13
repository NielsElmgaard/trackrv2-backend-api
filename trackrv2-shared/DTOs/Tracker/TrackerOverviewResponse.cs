namespace trackrv2_shared.DTOs;

public record TrackerOverviewResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime LastUpdated
);
