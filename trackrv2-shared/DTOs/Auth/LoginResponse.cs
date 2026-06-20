namespace trackrv2_shared.DTOs.Auth;

public record LoginResponse(string Username,string RefreshToken, int ExpiresIn);
