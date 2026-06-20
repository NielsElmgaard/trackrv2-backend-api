

namespace BadmintonKiosken.Api.Services.Auth;

public interface ILoginService
{
    Task<(string Token, LoginResponse Response)?> Login(LoginRequest request);
    Task InvalidateUserSessionAsync(string? refreshTokenFromCookie);
}