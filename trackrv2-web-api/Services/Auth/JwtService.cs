using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames =
    Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;
using trackrv2_efc;
using trackrv2_shared.DTOs.Auth;
using System.IdentityModel.Tokens.Jwt;
using trackrv2_shared;


namespace trackrv2_web_api.Services.Auth;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    private readonly IPasswordHasher<trackrv2_efc.Entities.User>
        _passwordHasher;

    private readonly TrackrContext _ctx;

    public JwtService(TrackrContext ctx,
        IPasswordHasher<trackrv2_efc.Entities.User> passwordHasher,
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

        if (request.SelectedRole.HasValue && !user.Roles.HasFlag(request.SelectedRole.Value))
        {
            throw new UnauthorizedAccessException($"{user.Username} har ikke rettigheder til den valgte rolle.");
        }

        var accessToken = CreateAccessTokenWithSpecificRole(user, request.SelectedRole, out DateTime expiryTime, out string activeRoleString);
        var refreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // letters and numbers only

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime =
            DateTime.UtcNow
                .AddDays(
                    7); // if no activity from customer in 7 days, logs the user out
        _ctx.Users.Update(user);
        await _ctx.SaveChangesAsync();


        var loginResponse = new LoginResponse
        (
            request.Username,
            refreshToken,
            (int)expiryTime.Subtract(DateTime.UtcNow).TotalSeconds,
            activeRoleString
        );

        return (accessToken, loginResponse);
    }

    public async Task<(string Token, LoginResponse Response)?> RefreshToken(
        RefreshRequest request)
    {
        // Find user based on refresh token
        var user = await _ctx.Users.FirstOrDefaultAsync(u =>
            u.Username == request.Username && u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new SecurityTokenException(
                "Ugyldigt eller udløbet refresh token");
        }

        var accessToken = CreateAccessTokenWithSpecificRole(user, request.SelectedRole, out DateTime expiryTime, out string activeRoleString);

        var newRefreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // Make a new token

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Reset the 7 days inactivity
        _ctx.Users.Update(user);
        await _ctx.SaveChangesAsync();

        var loginResponse = new LoginResponse
        (
            user.Username,
            newRefreshToken,
            (int)expiryTime.Subtract(DateTime.UtcNow).TotalSeconds,
            activeRoleString
        );

        return (accessToken, loginResponse);
    }

    public async Task<(string Token, LoginResponse Response)?> SwitchRole(Guid userId, SwitchRoleRequest request)
    {
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        // Does the user has the role
        if (request.TargetRole != Role.None && !user.Roles.HasFlag(request.TargetRole))
        {
            return null;
        }

        // if true, it is a multi-role request => pass null to accessToken
        bool isComposite = Enum.GetValues(typeof(Role)).Cast<Role>().Count(r => r != Role.None && request.TargetRole.HasFlag(r)) > 1;

        var accessToken = CreateAccessTokenWithSpecificRole(user, isComposite ? null : request.TargetRole, out DateTime expiryTime, out string activeRoleString);
        var newRefreshToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _ctx.Users.Update(user);
        await _ctx.SaveChangesAsync();

        var loginResponse = new LoginResponse
        (
            user.Username,
            newRefreshToken,
            (int)expiryTime.Subtract(DateTime.UtcNow).TotalSeconds,
            activeRoleString
        );

        return (accessToken, loginResponse);
    }

    // Redacted. Helper method to make secure random string for refresh token
    private string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }


    private string CreateAccessTokenWithSpecificRole(trackrv2_efc.Entities.User user, Role? selectedRole, out DateTime expiryTime, out string activeRoleString)
    {
        var issuer = _configuration["JwtConfig:Issuer"];
        var audience = _configuration["JwtConfig:Audience"];
        var key = _configuration["JwtConfig:Key"];
        var tokenValidityMins = _configuration.GetValue<int>("JwtConfig:TokenValidityMins", 15);

        expiryTime = DateTime.UtcNow.AddMinutes(tokenValidityMins);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        // Specific Role
        if (selectedRole.HasValue && selectedRole.Value != Role.None)
        {
            claims.Add(new Claim(ClaimTypes.Role, selectedRole.Value.ToString()));
            activeRoleString = selectedRole.Value.ToString();
        }
        else
        {
            // All roles (seletedRole is null)
            var activeRolesList = new List<string>();
            foreach (Role role in Enum.GetValues(typeof(Role)))
            {
                if (role != Role.None && user.Roles.HasFlag(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                    activeRolesList.Add(role.ToString());
                }
            }

            // Roles combined
            activeRoleString = string.Join(", ", activeRolesList);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
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