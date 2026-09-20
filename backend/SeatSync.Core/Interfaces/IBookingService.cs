using SeatSync.Core.DTOs;

namespace SeatSync.Core.Interfaces;

public interface IBookingService
{
    /// <summary>
    /// Publishes a confirm-booking message and returns immediately —
    /// does not wait for the DB write. The actual Booking row is created
    /// asynchronously by the background consumer.
    /// </summary>
    Task<ConfirmBookingAcceptedResponse> RequestConfirmationAsync(Guid userId, ConfirmBookingRequest request);

    Task<List<BookingResponse>> GetMyBookingsAsync(Guid userId);
    Task<BookingResponse?> GetByRequestIdAsync(Guid userId, Guid bookingRequestId);
    Task<byte[]?> GenerateReceiptAsync(Guid userId, Guid bookingId);
}