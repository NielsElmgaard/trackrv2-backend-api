using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trackrv2_shared.DTOs;
using trackrv2_web_api.Services.TrackerService;

namespace trackrv2_web_api.Controllers.Tracker;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TrackersController(ITrackerService trackerService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<TrackerDetailedResponse>> CreateTrackerAsync(
        TrackerRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await trackerService.CreateTrackerAsync(Guid.Parse(userIdStr), request);

        return CreatedAtAction(nameof(GetTrackerByIdAsync), new { trackerId = result.Id },
            result);
    }

    [HttpGet("{trackerId:Guid}")]
    public async Task<ActionResult<TrackerDetailedResponse>>
    GetTrackerByIdAsync(Guid trackerId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await trackerService.GetTrackerByIdAsync(trackerId, Guid.Parse(userIdStr));

        return Ok(result);
    }
}