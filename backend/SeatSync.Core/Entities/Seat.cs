namespace SeatSync.Core.Entities;

public class Seat
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }

    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }

    public SeatStatus Status { get; set; } = SeatStatus.Available;

    public decimal Price { get; set; }

    // Navigation
    public Event? Event { get; set; }
}