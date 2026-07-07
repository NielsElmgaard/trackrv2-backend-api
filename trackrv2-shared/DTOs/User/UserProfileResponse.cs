namespace trackrv2_shared.DTOs.User;

public record UserProfileResponse(
    Guid Id,
    string Username,
    string FirstName,
    string MiddleName,
    string LastName,
    string Email,
    long PhoneNumber,
    string? Nationality,
    Role Role,
    DateTime CreatedAt,
    DateTime LastUpdated,
    IEnumerable<TrackerOverviewResponse> SavedTrackers
);
