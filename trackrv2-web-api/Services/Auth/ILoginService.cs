using trackrv2_shared.DTOs.Auth;

namespace trackrv2_web_api.Services.Auth;

public interface ILoginService
{
    Task<(string Token, LoginResponse Response)?> Login(LoginRequest request);
    Task InvalidateUserSessionAsync(string? refreshTokenFromCookie);
}