using System.Net;
using BadmintonKiosken.Core;
using BadmintonKiosken.Shared.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using trackrv2_web_api.Services.Auth;

namespace BadmintonKiosken.Api.Services.Auth;

public class LoginService : ILoginService
{
    private readonly ILogger<LoginService> _logger;
    private readonly IJwtService _jwtService;
    private readonly BadmintonKioskenContext _ctx;

    public LoginService(ILogger<LoginService> logger, IJwtService jwtService,
        BadmintonKioskenContext ctx)
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

        var customer =
            await _ctx.Customers.FirstOrDefaultAsync(u =>
                u.RefreshToken == decodedToken);
        if (customer != null)
        {
            _logger.LogInformation(
                "Invaliderer refresh token session for bruger: {Username}",
                customer.Username);

            customer.RefreshToken = null;
            customer.RefreshTokenExpiryTime = null;

            _ctx.Customers.Update(customer);
            await _ctx.SaveChangesAsync();
        }
    }
}