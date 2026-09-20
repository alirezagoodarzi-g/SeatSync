using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatSync.Api.Extensions;
using SeatSync.Core.Interfaces;

namespace SeatSync.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/seats")]
[Authorize] // any authenticated user (Customer or Organizer) can hold a seat
public class SeatsController : ControllerBase
{
    private readonly IHoldService _holdService;

    public SeatsController(IHoldService holdService)
    {
        _holdService = holdService;
    }

    [HttpPost("{seatId:guid}/hold")]
    public async Task<IActionResult> Hold(Guid eventId, Guid seatId)
    {
        var userId = User.GetUserId();
        var result = await _holdService.HoldSeatAsync(eventId, seatId, userId);

        if (result is null)
        {
            return Conflict(new { error = "This seat is already held by another user." });
        }

        return Ok(result);
    }
    [HttpDelete("{seatId:guid}/hold")]
    public async Task<IActionResult> ReleaseHold(Guid eventId, Guid seatId)
    {
        var userId = User.GetUserId();
        await _holdService.ReleaseSeatAsync(eventId, seatId, userId);
        return NoContent();
    }
}