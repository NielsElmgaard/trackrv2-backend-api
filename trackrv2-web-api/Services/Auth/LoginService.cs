using System.Net;
using Microsoft.EntityFrameworkCore;
using trackrv2_efc;
using trackrv2_shared.DTOs.Auth;

namespace trackrv2_web_api.Services.Auth;

public class LoginService : ILoginService
{
    private readonly ILogger<LoginService> _logger;
    private readonly IJwtService _jwtService;
    private readonly TrackrContext _ctx;

    public LoginService(ILogger<LoginService> logger, IJwtService jwtService,
        TrackrContext ctx)
    {
        _logger = logger;
        _jwtService = jwtService;
        _ctx = ctx;
    }

    public async Task<(string Token, LoginResponse Response)?> Login(
        LoginRequest request)
    {
        _logger.LogInformation("{RequestUsername} forsøgte at logge ind.",
            request.Username);

        var result = await _jwtService.Authenticate(request);

        _logger.LogInformation("Bruger {Username} loggede ind succesfuldt.",
            request.Username);

        return result;
    }

    public async Task InvalidateUserSessionAsync(string? refreshTokenFromCookie)
    {
        if (string.IsNullOrEmpty(refreshTokenFromCookie)) return;

        var decodedToken = WebUtility.UrlDecode(refreshTokenFromCookie);

        var user =
            await _ctx.Users.FirstOrDefaultAsync(u =>
                u.RefreshToken == decodedToken);
        if (user != null)
        {
            _logger.LogInformation(
                "Invaliderer refresh token session for bruger: {Username}",
                user.Username);

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            _ctx.Users.Update(user);
            await _ctx.SaveChangesAsync();
        }
    }
}