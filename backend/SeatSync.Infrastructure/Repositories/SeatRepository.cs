using Microsoft.EntityFrameworkCore;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;
using SeatSync.Infrastructure.Data;

namespace SeatSync.Infrastructure.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly SeatSyncDbContext _context;

    public SeatRepository(SeatSyncDbContext context)
    {
        _context = context;
    }

    public async Task<Seat?> GetByIdAsync(Guid id)
        => await _context.Seats.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<Seat>> GetByEventIdAsync(Guid eventId)
        => await _context.Seats
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Row).ThenBy(s => s.Number)
            .ToListAsync();

    public async Task<List<Seat>> GetByIdsAsync(IEnumerable<Guid> ids)
        => await _context.Seats.Where(s => ids.Contains(s.Id)).ToListAsync();

    public async Task AddRangeAsync(IEnumerable<Seat> seats)
    {
        _context.Seats.AddRange(seats);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid seatId, SeatStatus status)
    {
        var seat = await _context.Seats.FirstOrDefaultAsync(s => s.Id == seatId);
        if (seat is null) return;

        seat.Status = status;
        await _context.SaveChangesAsync();
    }
}