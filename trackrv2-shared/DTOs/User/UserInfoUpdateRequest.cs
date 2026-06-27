namespace trackrv2_shared.DTOs.User;

public record UserInfoUpdateRequest(
    string Username,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Nationality,
    string Email,
    long PhoneNumber);