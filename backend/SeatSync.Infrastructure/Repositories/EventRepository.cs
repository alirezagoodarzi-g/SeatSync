using Microsoft.EntityFrameworkCore;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;
using SeatSync.Infrastructure.Data;

namespace SeatSync.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly SeatSyncDbContext _context;

    public EventRepository(SeatSyncDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(Guid id)
        => await _context.Events.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Event?> GetByIdWithSeatsAsync(Guid id)
        => await _context.Events
            .Include(e => e.Seats)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<List<Event>> GetAllAsync()
        => await _context.Events
            .Include(e => e.Seats)
            .OrderBy(e => e.DateTime)
            .ToListAsync();

    public async Task<List<Event>> GetByOrganizerIdAsync(Guid organizerId)
        => await _context.Events
            .Include(e => e.Seats)
            .Where(e => e.OrganizerId == organizerId)
            .OrderBy(e => e.DateTime)
            .ToListAsync();

    public async Task<Event> CreateAsync(Event @event)
    {
        _context.Events.Add(@event);
        await _context.SaveChangesAsync();
        return @event;
    }
}