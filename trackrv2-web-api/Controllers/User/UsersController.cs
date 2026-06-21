using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trackrv2_shared;
using trackrv2_shared.DTOs.User;
using trackrv2_web_api.Services.User;

namespace trackrv2_web_api.Controllers.User;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController(IUserService userService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<UserProfileResponse>> RegisterUser(
        UserRequest request)
    {
        var result = await userService.RegisterUser(request);

        return CreatedAtAction(nameof(GetUserById), new { id = result.Id },
            result);
    }

    [Authorize]
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<UserProfileResponse>>
        GetUserById(Guid id)
    {
        var result = await userService.GetUserById(id);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("search")]
    public async Task<ActionResult<UserProfileResponse>> GetSearchUser(
        [FromQuery] SingleUserSearchRequest searchRequest)
    {
        if (string.IsNullOrWhiteSpace(searchRequest.Username) &&
            string.IsNullOrWhiteSpace(searchRequest.Email) &&
            searchRequest.PhoneNumber == 0)
        {
            return BadRequest(
                "Du skal angive enten 'brugernavn', 'e-mail' eller 'telefonnummer', når du søger efter enkel kunde");
        }

        var user =
            await userService.GetSingleUserBySearch(searchRequest);
        return Ok(user);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<UserOverviewResponse>>>
        GetUsers(
            [FromQuery] Guid? id,
            [FromQuery] string? username,
            [FromQuery] string? fullName,
            [FromQuery] string? email,
            [FromQuery] long? phoneNumber,
            [FromQuery] string? nationality,
            [FromQuery] Role? role,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? lastUpdated)
    {
        var users = await userService.GetUsers(id, username, fullName,
            email, phoneNumber, nationality, role, createdAt,
            lastUpdated);
        return Ok(users);
    }
}