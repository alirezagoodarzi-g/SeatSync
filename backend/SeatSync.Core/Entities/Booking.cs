namespace SeatSync.Core.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Correlates this row back to the original confirm-booking message.
    // Used by the consumer to detect and safely ignore redelivered messages.
    public Guid BookingRequestId { get; set; }

    public Guid UserId { get; set; }
    public Guid EventId { get; set; }

    public List<Guid> SeatIds { get; set; } = new();

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    // Navigation
    public User? User { get; set; }
    public Event? Event { get; set; }
}