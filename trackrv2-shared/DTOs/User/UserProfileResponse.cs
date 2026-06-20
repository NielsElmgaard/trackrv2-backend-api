namespace trackrv2_shared.DTOs.User;

public record UserProfileResponse(
    Guid Id,
    string Username,
    string FullName,
    string Email,
    long PhoneNumber,
    string? Nationality,
    Role Role,
    DateTime CreatedAt,
    DateTime LastUpdated

);