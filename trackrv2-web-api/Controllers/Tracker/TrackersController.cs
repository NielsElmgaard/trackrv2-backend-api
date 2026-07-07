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
        [FromBody] TrackerRequest request
    )
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        var result = await trackerService.CreateTrackerAsync(userId, request);

        return CreatedAtRoute("GetTrackerById", new { trackerId = result.Id }, result);
    }

    [HttpDelete("{trackerId:Guid}")]
    public async Task<ActionResult> DeleteTrackerAsync([FromRoute] Guid trackerId)
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        await trackerService.DeleteTrackerAsync(trackerId, userId);

        return NoContent();
    }

    [HttpGet("{trackerId:Guid}", Name = "GetTrackerById")]
    public async Task<ActionResult<TrackerDetailedResponse>> GetTrackerByIdAsync(
        [FromRoute] Guid trackerId
    )
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        var result = await trackerService.GetTrackerByIdAsync(trackerId, userId);

        return Ok(result);
    }

    [HttpPut("{trackerId:Guid}")]
    public async Task<ActionResult> UpdateTrackerAsync(
        [FromRoute] Guid trackerId,
        [FromBody] TrackerRequest request
    )
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        await trackerService.UpdateTrackerAsync(trackerId, userId, request);

        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<TrackerOverviewResponse>> GetTrackersByUserAsync(
        [FromQuery] string? name,
        [FromQuery] DateTime? createdAt,
        [FromQuery] DateTime? lastUpdated
    )
    {
        var userIdStr = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        var userId = Guid.Parse(userIdStr!);

        var result = await trackerService.GetTrackersByUserAsync(
            userId,
            name,
            createdAt,
            lastUpdated
        );

        return Ok(result);
    }
}
