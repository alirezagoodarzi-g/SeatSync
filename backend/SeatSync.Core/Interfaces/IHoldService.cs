using SeatSync.Core.DTOs;

namespace SeatSync.Core.Interfaces;

public interface IHoldService
{
    /// <summary>
    /// Returns the hold response if acquired, or null if the seat is already held.
    /// </summary>
    Task<HoldSeatResponse?> HoldSeatAsync(Guid eventId, Guid seatId, Guid userId);
    Task ReleaseSeatAsync(Guid eventId, Guid seatId, Guid userId);
}