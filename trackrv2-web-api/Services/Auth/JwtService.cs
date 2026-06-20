using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames =
    Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;
using System.Net;
using trackrv2_web_api.Services.Auth;
using trackrv2_efc;
using trackrv2_efc.Entities;
using trackrv2_shared.DTOs.Auth;


namespace BadmintonKiosken.Api.Services.Auth;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    private readonly IPasswordHasher<User>
        _passwordHasher;

    private readonly TrackrContext _ctx;

    public JwtService(TrackrContext ctx,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        _ctx = ctx;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<(string Token, LoginResponse Response)?> Authenticate(
        LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ApplicationException(
                "Brugernavn og adgangskode skal udfyldes.");
        }

        var user =
            await _ctx.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
        {
            throw new UnauthorizedAccessException(
                "Ugyldigt brugernavn eller adgangskode"); // wrong username
        }

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(user, user.Password,
                request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(
                "Ugyldigt brugernavn eller adgangskode"); // Wrong password
        }

        var accessToken = CreateAccessToken(request.Username, user.LoyaltyGroup.ToString(), out DateTime expiryTime);
        var refreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // letters and numbers only

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime =
            DateTime.UtcNow
                .AddDays(
                    7); // if no activity from customer in 7 days, logs the user out
        _ctx.Customers.Update(user);
        await _ctx.SaveChangesAsync();


        var loginResponse = new LoginResponse
        (
            request.Username,
            refreshToken,
            (int)expiryTime.Subtract(DateTime.UtcNow).TotalSeconds
        );

        return (accessToken, loginResponse);
    }

    public async Task<(string Token, LoginResponse Response)?> RefreshToken(
        string tokenFromCookie)
    {
        var decodedToken = WebUtility.UrlDecode(tokenFromCookie);

        // Find customer based on refresh token
        var customer =
            await _ctx.Customers.FirstOrDefaultAsync(u =>
                u.RefreshToken == decodedToken);

        if (customer == null || customer.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new SecurityTokenException(
                "Ugyldigt eller udløbet refresh token");
        }

        var accessToken = CreateAccessToken(customer.Username, customer.LoyaltyGroup.ToString(), out DateTime expiryTime);

        var newRefreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // Make a new token

        customer.RefreshToken = newRefreshToken;
        customer.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Reset the 7 days inactivity
        _ctx.Customers.Update(customer);
        await _ctx.SaveChangesAsync();

        var loginResponse = new LoginResponse
        (
            customer.Username,
            newRefreshToken,
            (int)expiryTime.Subtract(DateTime.UtcNow).TotalSeconds
        );

        return (accessToken, loginResponse);
    }

    // Helper method to make secure random string for refresh token
    private string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }


    private string CreateAccessToken(string username, string role, out DateTime expiryTime)
    {
        var issuer = _configuration["JwtConfig:Issuer"];
        var audience = _configuration["JwtConfig:Audience"];
        var key = _configuration["JwtConfig:Key"];
        var tokenValidityMins = _configuration.GetValue<int>("JwtConfig:TokenValidityMins", 15);

        expiryTime = DateTime.UtcNow.AddMinutes(tokenValidityMins);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(JwtRegisteredClaimNames.Name, username),
            new Claim(ClaimTypes.Role, role)
        }),
            Expires = expiryTime,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
                SecurityAlgorithms.HmacSha512Signature),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}