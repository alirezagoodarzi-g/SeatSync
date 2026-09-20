using SeatSync.Core.DTOs;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;

namespace SeatSync.Core.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ISeatRepository _seatRepository;

    public EventService(IEventRepository eventRepository, ISeatRepository seatRepository)
    {
        _eventRepository = eventRepository;
        _seatRepository = seatRepository;
    }

    public async Task<EventResponse> CreateEventAsync(Guid organizerId, CreateEventRequest request)
    {
        var newEvent = new Event
        {
            Name = request.Name,
            Venue = request.Venue,
            DateTime = request.DateTime,
            OrganizerId = organizerId
        };

        await _eventRepository.CreateAsync(newEvent);

        var seats = request.Seats.Select(s => new Seat
        {
            EventId = newEvent.Id,
            Row = s.Row,
            Number = s.Number,
            Price = s.Price,
            Status = SeatStatus.Available
        }).ToList();

        await _seatRepository.AddRangeAsync(seats);

        return MapToResponse(newEvent, seats);
    }

    public async Task<EventResponse?> GetEventByIdAsync(Guid eventId)
    {
        var @event = await _eventRepository.GetByIdWithSeatsAsync(eventId);
        return @event is null ? null : MapToResponse(@event, @event.Seats);
    }

    public async Task<List<EventSummaryResponse>> GetAllEventsAsync()
    {
        var events = await _eventRepository.GetAllAsync();
        return events.Select(MapToSummary).ToList();
    }

    public async Task<List<EventSummaryResponse>> GetEventsByOrganizerAsync(Guid organizerId)
    {
        var events = await _eventRepository.GetByOrganizerIdAsync(organizerId);
        return events.Select(MapToSummary).ToList();
    }

    private static EventResponse MapToResponse(Event @event, List<Seat> seats)
        => new(
            @event.Id,
            @event.Name,
            @event.Venue,
            @event.DateTime,
            @event.OrganizerId,
            seats.Select(s => new SeatResponse(s.Id, s.Row, s.Number, s.Price, s.Status.ToString())).ToList()
        );

    private static EventSummaryResponse MapToSummary(Event @event)
        => new(@event.Id, @event.Name, @event.Venue, @event.DateTime, @event.Seats.Count);
}