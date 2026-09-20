namespace SeatSync.Core.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }

    public Guid OrganizerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<Seat> Seats { get; set; } = new();
}