using Microsoft.EntityFrameworkCore;
using SeatSync.Core.Entities;

namespace SeatSync.Infrastructure.Data;

public class SeatSyncDbContext : DbContext
{
    public SeatSyncDbContext(DbContextOptions<SeatSyncDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Venue).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.OrganizerId);

            entity.HasMany(e => e.Seats)
                  .WithOne(s => s.Event)
                  .HasForeignKey(s => s.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Row).IsRequired().HasMaxLength(10);
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.Price).HasColumnType("numeric(10,2)");

            // A given row+number can only exist once per event.
            entity.HasIndex(s => new { s.EventId, s.Row, s.Number }).IsUnique();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(b => b.UserId);
            entity.HasIndex(b => b.EventId);
            entity.HasIndex(b => b.BookingRequestId).IsUnique();

            // Native Postgres uuid[] column instead of a join table —
            // simpler for this project's size, and a booking's seats are
            // never queried individually, only as a whole set.
            entity.Property(b => b.SeatIds).HasColumnType("uuid[]");

            entity.HasOne(b => b.User)
                  .WithMany(u => u.Bookings)
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Event)
                  .WithMany()
                  .HasForeignKey(b => b.EventId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}