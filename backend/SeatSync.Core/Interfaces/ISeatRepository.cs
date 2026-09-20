using SeatSync.Core.Entities;

namespace SeatSync.Core.Interfaces;

public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(Guid id);
    Task<List<Seat>> GetByEventIdAsync(Guid eventId);
    Task<List<Seat>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task AddRangeAsync(IEnumerable<Seat> seats);
    Task UpdateStatusAsync(Guid seatId, SeatStatus status);
}