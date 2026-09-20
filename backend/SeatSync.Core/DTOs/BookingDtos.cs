namespace SeatSync.Core.DTOs;

public record ConfirmBookingRequest(Guid EventId, List<Guid> SeatIds);

public record ConfirmBookingAcceptedResponse(Guid BookingRequestId, string Status);

public record BookingResponse(
    Guid Id,
    Guid EventId,
    List<Guid> SeatIds,
    string Status,
    DateTime CreatedAt,
    DateTime? ConfirmedAt
);