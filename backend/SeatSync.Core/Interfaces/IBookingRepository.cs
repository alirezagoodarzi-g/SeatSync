using SeatSync.Core.Entities;

namespace SeatSync.Core.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<List<Booking>> GetByUserIdAsync(Guid userId);
    Task<Booking> CreateAsync(Booking booking);
    Task UpdateStatusAsync(Guid bookingId, BookingStatus status, DateTime? confirmedAt = null);
    Task<bool> ExistsByRequestIdAsync(Guid bookingRequestId);
    Task<Booking?> GetByRequestIdAsync(Guid bookingRequestId);
}