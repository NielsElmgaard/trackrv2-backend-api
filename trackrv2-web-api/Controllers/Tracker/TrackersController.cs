using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trackrv2_shared.DTOs;
using trackrv2_web_api.Services.TrackerService;

namespace trackrv2_web_api.Controllers.Tracker;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "UserOnly")]

public class TrackersController(ITrackerService trackerService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TrackerDetailedResponse>> CreateTrackerAsync(
        TrackerRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);
        var result = await trackerService.CreateTrackerAsync(userId, request);

        return CreatedAtAction(nameof(GetTrackerByIdAsync), new { trackerId = result.Id },
            result);
    }


    [HttpDelete("{trackerId:Guid}")]
    public async Task<ActionResult> DeleteTrackerAsync([FromRoute] Guid trackerId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        await trackerService.DeleteTrackerAsync(trackerId, userId);

        return NoContent();
    }

    [HttpGet("{trackerId:Guid}")]
    public async Task<ActionResult<TrackerDetailedResponse>>
    GetTrackerByIdAsync([FromRoute] Guid trackerId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        var result = await trackerService.GetTrackerByIdAsync(trackerId, userId);

        return Ok(result);
    }

    [HttpPut("{trackerId:Guid}")]
    public async Task<ActionResult> UpdateTrackerNameAsync([FromRoute] Guid trackerId, [FromBody] string newName)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        await trackerService.UpdateTrackerNameAsync(trackerId, userId, newName);

        return NoContent();
    }
}