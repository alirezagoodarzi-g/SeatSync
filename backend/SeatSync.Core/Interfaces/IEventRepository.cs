using SeatSync.Core.Entities;

namespace SeatSync.Core.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<Event?> GetByIdWithSeatsAsync(Guid id);
    Task<List<Event>> GetAllAsync();
    Task<List<Event>> GetByOrganizerIdAsync(Guid organizerId);
    Task<Event> CreateAsync(Event @event);
}