using SeatSync.Core.DTOs;

namespace SeatSync.Core.Interfaces;

public interface IEventService
{
    Task<EventResponse> CreateEventAsync(Guid organizerId, CreateEventRequest request);
    Task<EventResponse?> GetEventByIdAsync(Guid eventId);
    Task<List<EventSummaryResponse>> GetAllEventsAsync();
    Task<List<EventSummaryResponse>> GetEventsByOrganizerAsync(Guid organizerId);
}