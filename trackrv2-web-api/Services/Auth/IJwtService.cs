
using trackrv2_shared.DTOs.Auth;

namespace trackrv2_web_api.Services.Auth;

public interface IJwtService
{
    Task<(string Token, LoginResponse Response)?> Authenticate(LoginRequest request);
    Task<(string Token, LoginResponse Response)?> RefreshToken(string tokenFromCookie);

}