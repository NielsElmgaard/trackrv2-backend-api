namespace trackrv2_shared.DTOs.Auth;

public record LoginRequest(string? Username, string? Password, Role? SelectedRole = null);
