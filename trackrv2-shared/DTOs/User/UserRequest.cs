namespace trackrv2_shared.DTOs.User;

public record UserRequest(
    string Username,
    string Password,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Nationality,
    string Email,
    long PhoneNumber
);
