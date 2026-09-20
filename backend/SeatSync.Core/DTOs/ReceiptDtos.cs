namespace SeatSync.Core.DTOs;

public record ReceiptSeatLine(string RowNumber, decimal Price);

public record ReceiptData(
    string EventName,
    string Venue,
    DateTime EventDateTimeUtc,
    string CustomerName,
    List<ReceiptSeatLine> Seats,
    decimal TotalPrice
);