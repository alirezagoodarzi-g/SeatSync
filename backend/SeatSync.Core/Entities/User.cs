namespace SeatSync.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    // BCrypt hash — never store or transmit plaintext passwords.
    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.Customer;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<Booking> Bookings { get; set; } = new();
}