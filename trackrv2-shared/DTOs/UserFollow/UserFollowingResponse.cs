namespace trackrv2_shared.DTOs;

public record UserFollowingResponse(
    string Username,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Nationality,
    DateTime? FollowingAt
);
