namespace trackrv2_shared.DTOs.User;

public record SearchUserResponse(
    Guid Id,
    string Username,
    string FirstName,
    string MiddleName,
    string LastName,
    string? Nationality,
    DateTime CreatedAt
);
