using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trackrv2_shared.DTOs;
using trackrv2_web_api.Services.IUserFollowService;

namespace trackrv2_web_api.Controllers.UserFollow;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UserFollowsController(IUserFollowService userFollowService) : ControllerBase
{
    [HttpPost("{followingId:Guid}")]
    public async Task<ActionResult<FollowResponse>> FollowUser([FromRoute] Guid followingId)
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        var result = await userFollowService.FollowUserAsync(userId, followingId);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("followers")]
    public async Task<ActionResult<UserFollowerResponse>> GetFollowersForUser(
        [FromQuery] string? userName,
        [FromQuery] string? firstName,
        [FromQuery] string? middleName,
        [FromQuery] string? lastName,
        [FromQuery] string? nationality,
        [FromQuery] DateTime? followedAt
    )
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        var result = await userFollowService.GetFollowersForUserAsync(
            userId,
            userName,
            firstName,
            middleName,
            lastName,
            nationality,
            followedAt
        );

        return Ok(result);
    }

    [HttpGet("following")]
    public async Task<ActionResult<UserFollowingResponse>> GetFollowingsForUser(
        [FromQuery] string? userName,
        [FromQuery] string? firstName,
        [FromQuery] string? middleName,
        [FromQuery] string? lastName,
        [FromQuery] string? nationality,
        [FromQuery] DateTime? followingAt
    )
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        var result = await userFollowService.GetFollowingsForUserAsync(
            userId,
            userName,
            firstName,
            middleName,
            lastName,
            nationality,
            followingAt
        );

        return Ok(result);
    }

    [HttpDelete("{followingId:Guid}")]
    public async Task<ActionResult> UnFollowUserAsync([FromRoute] Guid followingId)
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        await userFollowService.UnFollowUserAsync(userId, followingId);

        return NoContent();
    }
}
