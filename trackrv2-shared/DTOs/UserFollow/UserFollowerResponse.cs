namespace trackrv2_shared.DTOs;

public record UserFollowerResponse
(
    string Username,
    string FirstName,
    string MiddleName,
    string LastName,
    string? Nationality,
    DateTime? FollowedAt
);