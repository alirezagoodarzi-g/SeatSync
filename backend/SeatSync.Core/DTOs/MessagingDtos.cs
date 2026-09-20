namespace SeatSync.Core.DTOs;

public record ConfirmBookingMessage(
    Guid BookingRequestId,
    Guid UserId,
    Guid EventId,
    List<Guid> SeatIds
);