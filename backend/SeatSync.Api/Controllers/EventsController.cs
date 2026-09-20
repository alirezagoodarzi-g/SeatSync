using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatSync.Api.Extensions;
using SeatSync.Core.DTOs;
using SeatSync.Core.Interfaces;

namespace SeatSync.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    // Anyone (even unauthenticated) can browse events — a customer needs
    // this before they've logged in to decide whether to sign up.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var events = await _eventService.GetAllEventsAsync();
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var @event = await _eventService.GetEventByIdAsync(id);
        return @event is null ? NotFound() : Ok(@event);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Organizer")]
    public async Task<IActionResult> GetMyEvents()
    {
        var organizerId = User.GetUserId();
        var events = await _eventService.GetEventsByOrganizerAsync(organizerId);
        return Ok(events);
    }

    [HttpPost]
    [Authorize(Roles = "Organizer")]
    public async Task<IActionResult> Create(CreateEventRequest request)
    {
        var organizerId = User.GetUserId();
        var created = await _eventService.CreateEventAsync(organizerId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}