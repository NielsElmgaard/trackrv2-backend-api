using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trackrv2_shared.DTOs.Auth;
using trackrv2_web_api.Services.Auth;

namespace trackrv2_web_api.Controllers.Auth;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly ILoginService _loginService;
    private readonly IJwtService _jwtService;

    public AuthController(ILoginService loginService, IJwtService jwtService)
    {
        _loginService = loginService;
        _jwtService = jwtService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request)
    {
        var result = await _loginService.Login(request);
        setRefreshTokenCookie(result!.Value.Response.RefreshToken);
        Response.Headers.Append("Authorization", $"Bearer {result!.Value.Token}");
        Response.Headers.Append("Access-Control-Expose-Headers", "Authorization");

        return Ok(new { username = result.Value.Response.Username });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["X-Refresh-Token"];
        await _loginService.InvalidateUserSessionAsync(refreshToken);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1), // Set to yesterday
            Path = "/"
        };

        Response.Cookies.Append("X-Refresh-Token", "", cookieOptions);
        return Ok(new { message = "Loggede ud succesfuldt" });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var cookieRefreshToken = Request.Cookies["X-Refresh-Token"];

        if (string.IsNullOrEmpty(cookieRefreshToken))
            return Unauthorized("Mangler refresh token i cookies");

        var secureRequest = request with { RefreshToken = cookieRefreshToken };

        var result = await _jwtService.RefreshToken(secureRequest);

        if (result == null)
            return Unauthorized("Ugyldigt eller udløbet refresh token");

        setRefreshTokenCookie(result!.Value.Response.RefreshToken);
        Response.Headers.Append("Authorization", $"Bearer {result!.Value.Token}");
        Response.Headers.Append("Access-Control-Expose-Headers", "Authorization");

        return Ok(new { username = result.Value.Response.Username });
    }
    [HttpPost("switch-role")]
    public async Task<IActionResult> SwitchRoleAsync([FromBody] SwitchRoleRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);
        var result = await _jwtService.SwitchRole(userId, request);

        if (result == null)
        {
            return Forbid($"Bruger med id'et {userId} har ikke rettigheder");
        }
        setRefreshTokenCookie(result.Value.Response.RefreshToken);
        Response.Headers.Append("Authorization", $"Bearer {result!.Value.Token}");
        Response.Headers.Append("Access-Control-Expose-Headers", "Authorization");
        return Ok(new { username = result.Value.Response.Username, activeRole = result.Value.Response.ActiveRole });
    }

    private void setRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // TODO: Set to true when in production
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/"
        };

        Response.Cookies.Append("X-Refresh-Token", refreshToken, cookieOptions);
    }
}