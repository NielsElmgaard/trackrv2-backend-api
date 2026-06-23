using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trackrv2_shared;
using trackrv2_shared.DTOs.User;
using trackrv2_web_api.Services.User;

namespace trackrv2_web_api.Controllers.User;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController(IUserService userService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<UserProfileResponse>> RegisterUser(
        UserRequest request)
    {
        var result = await userService.RegisterUser(request);

        return CreatedAtAction(nameof(GetUserById), new { id = result.Id },
            result);
    }

    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<UserProfileResponse>>
        GetUserById(Guid id)
    {
        var result = await userService.GetUserById(id);

        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
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

    [Authorize(Policy = "AdminOnly")]
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

    [Authorize(Policy = "AdminOnly")]

    [HttpDelete("force-delete/{userId:Guid}")]
    public async Task<ActionResult> ForceDeleteUser(
        Guid userId)
    {
        await userService.DeleteUserAsync(userId);

        return NoContent();
    }

    [Authorize(Policy = "UserOnly")]

    [HttpDelete("{id:Guid}")]
    public async Task<ActionResult> DeleteUser()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        await userService.DeleteUserAsync(Guid.Parse(userIdStr));

        return NoContent();
    }
}