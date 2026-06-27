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
        var result = await userService.RegisterUserAsync(request);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUserAsync([FromBody] UserInfoUpdateRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        await userService.UpdateUserAsync(userId, request);

        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]

    [HttpDelete("force-delete/{userId:Guid}")]
    public async Task<ActionResult> ForceDeleteUserAsync(
            [FromRoute] Guid userId)
    {
        await userService.DeleteUserAsync(userId);

        return NoContent();
    }

    [Authorize(Policy = "UserOnly")]

    [HttpDelete]
    public async Task<ActionResult> DeleteUserAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        await userService.DeleteUserAsync(userId);

        return NoContent();
    }

    [HttpPut("password")]
    public async Task<ActionResult> UpdateUserPasswordAsync([FromBody] UpdatePasswordRequest request) // secured with https
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        await userService.UpdateUserPasswordAsync(userId, request.Password);

        return NoContent();
    }


    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{userId:Guid}")]
    public async Task<ActionResult> UpdateUserRolesAsync(Guid userId, [FromBody] UpdateUserRolesRequest request)
    {
        await userService.UpdateUserRolesAsync(userId, request);

        return NoContent();
    }


    [Authorize(Policy = "UserOnly")]
    [HttpGet("user")]
    public async Task<ActionResult<UserProfileResponse>>
        GetSingleUserByIdAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);
        var result = await userService.GetUserByIdAsync(userId);

        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<UserProfileResponse>>
    GetSingleUserByIdAsAdminAsync([FromRoute] Guid id)
    {
        var result = await userService.GetUserByIdAsync(id);

        return Ok(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("search")]
    public async Task<ActionResult<UserProfileResponse>> GetSearchUserAsync(
        [FromQuery] SingleUserSearchRequest searchRequest)
    {
        if (string.IsNullOrWhiteSpace(searchRequest.Username) &&
            string.IsNullOrWhiteSpace(searchRequest.Email) &&
            searchRequest.PhoneNumber == 0)
        {
            return BadRequest(
                "Du skal angive enten 'brugernavn', 'e-mail' eller 'telefonnummer', når du søger efter enkel bruger");
        }

        var user =
            await userService.GetSingleUserBySearchAsync(searchRequest);
        return Ok(user);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<List<UserOverviewResponse>>>
        GetUsersAsync(
            [FromQuery] Guid? id,
            [FromQuery] string? username,
            [FromQuery] string? firstName,
            [FromQuery] string? middleName,
            [FromQuery] string? lastName,
            [FromQuery] string? email,
            [FromQuery] long? phoneNumber,
            [FromQuery] string? nationality,
            [FromQuery] Role? role,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? lastUpdated)
    {
        var users = await userService.GetUsersAsync(id, username, firstName,middleName,lastName,
            email, phoneNumber, nationality, role, createdAt,
            lastUpdated);
        return Ok(users);
    }


}