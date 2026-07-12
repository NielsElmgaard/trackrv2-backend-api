namespace trackrv2_shared.DTOs.User;

public record SingleUserSearchRequest(
    string? Username = null,
    string? Email = null,
    long? PhoneNumber = null
);
