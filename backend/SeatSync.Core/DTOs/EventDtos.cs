namespace SeatSync.Core.DTOs;

public record CreateSeatRequest(string Row, int Number, decimal Price);

public record CreateEventRequest(
    string Name,
    string Venue,
    DateTime DateTime,
    List<CreateSeatRequest> Seats
);

public record SeatResponse(Guid Id, string Row, int Number, decimal Price, string Status);

public record EventResponse(
    Guid Id,
    string Name,
    string Venue,
    DateTime DateTime,
    Guid OrganizerId,
    List<SeatResponse> Seats
);

public record EventSummaryResponse(
    Guid Id,
    string Name,
    string Venue,
    DateTime DateTime,
    int TotalSeats
);
public record HoldSeatResponse(Guid SeatId, DateTime HeldUntil);