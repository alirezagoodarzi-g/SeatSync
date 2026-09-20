namespace SeatSync.Core.Interfaces;

public interface ISeatLockService
{
    /// <summary>
    /// Attempts to acquire a hold on a seat for the given user.
    /// Returns true if the hold was acquired, false if the seat is already held.
    /// </summary>
    Task<bool> TryAcquireHoldAsync(Guid eventId, Guid seatId, Guid userId, TimeSpan holdDuration);

    /// <summary>
    /// Releases a hold — used on confirm (Phase 4) or explicit cancel.
    /// </summary>
    Task ReleaseHoldAsync(Guid eventId, Guid seatId);

    /// <summary>
    /// Returns the userId currently holding the seat, or null if unheld.
    /// </summary>
    Task<Guid?> GetHoldOwnerAsync(Guid eventId, Guid seatId);
}