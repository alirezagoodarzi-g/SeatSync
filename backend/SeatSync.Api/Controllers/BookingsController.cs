using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatSync.Api.Extensions;
using SeatSync.Core.DTOs;
using SeatSync.Core.Interfaces;

namespace SeatSync.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(ConfirmBookingRequest request)
    {
        var userId = User.GetUserId();
        var result = await _bookingService.RequestConfirmationAsync(userId, request);

        // 202 Accepted — deliberately not 200/201. The booking isn't
        // persisted yet at the moment this response goes out; it's queued.
        return Accepted(result);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = User.GetUserId();
        var bookings = await _bookingService.GetMyBookingsAsync(userId);
        return Ok(bookings);
    }
    [HttpGet("by-request/{bookingRequestId:guid}")]
    public async Task<IActionResult> GetByRequestId(Guid bookingRequestId)
    {
        var userId = User.GetUserId();
        var booking = await _bookingService.GetByRequestIdAsync(userId, bookingRequestId);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> GetReceipt(Guid id)
    {
        var userId = User.GetUserId();
        var pdfBytes = await _bookingService.GenerateReceiptAsync(userId, id);
        if (pdfBytes is null) return NotFound();
        return File(pdfBytes, "application/pdf", "bilit-seatsync.pdf");
    }
}