using Microsoft.EntityFrameworkCore;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;
using SeatSync.Infrastructure.Data;

namespace SeatSync.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly SeatSyncDbContext _context;

    public BookingRepository(SeatSyncDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id)
        => await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<List<Booking>> GetByUserIdAsync(Guid userId)
        => await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<Booking> CreateAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task UpdateStatusAsync(Guid bookingId, BookingStatus status, DateTime? confirmedAt = null)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return;

        booking.Status = status;
        if (confirmedAt.HasValue) booking.ConfirmedAt = confirmedAt.Value;
        await _context.SaveChangesAsync();
    }
    public async Task<bool> ExistsByRequestIdAsync(Guid bookingRequestId)
    => await _context.Bookings.AnyAsync(b => b.BookingRequestId == bookingRequestId);
    public async Task<Booking?> GetByRequestIdAsync(Guid bookingRequestId)
    => await _context.Bookings.FirstOrDefaultAsync(b => b.BookingRequestId == bookingRequestId);
}