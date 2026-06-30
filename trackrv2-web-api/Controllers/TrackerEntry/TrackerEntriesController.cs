using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trackrv2_shared.DTOs;
using trackrv2_web_api.Services.TrackerEntryService;

namespace trackrv2_web_api.Controllers.TrackerEntry;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "UserOnly")]

public class TrackerEntriesController(ITrackerEntryService trackerEntryService) : ControllerBase
{
    [HttpPost("{trackerId:Guid}")]
    public async Task<ActionResult<TrackerEntryResponse>> CreateTrackerEntryAsync(
        [FromRoute] Guid trackerId, [FromBody] TrackerEntryRequest request)
    {

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);
        var result = await trackerEntryService.CreateTrackerEntryAsync(trackerId, userId, request);

        return CreatedAtRoute("GetTrackerEntriesForTracker", new { trackerId = trackerId }, result);

    }


    [HttpDelete("{trackerEntryId:Guid}")]
    public async Task<ActionResult> DeleteTrackerEntryAsync([FromRoute] Guid trackerEntryId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        await trackerEntryService.DeleteTrackerEntryAsync(trackerEntryId, userId);

        return NoContent();
    }

    [HttpGet("{trackerId:Guid}", Name = "GetTrackerEntriesForTracker")]
    public async Task<ActionResult<TrackerDetailedResponse>>
    GetTrackerEntriesForTrackerAsync([FromRoute] Guid trackerId, [FromQuery] DateTime? fromCreatedAtDate, [FromQuery] DateTime? toCreatedAtDate)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        var result = await trackerEntryService.GetTrackerEntriesForTrackerAsync(trackerId, userId, fromCreatedAtDate, toCreatedAtDate);

        return Ok(result);
    }

    [HttpPut("{trackerEntryId:Guid}")]
    public async Task<ActionResult> UpdateTrackerEntryAsync([FromRoute] Guid trackerEntryId, [FromBody] TrackerEntryRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = Guid.Parse(userIdStr);

        await trackerEntryService.UpdateTrackerEntryAsync(trackerEntryId, userId, request);

        return NoContent();
    }
}