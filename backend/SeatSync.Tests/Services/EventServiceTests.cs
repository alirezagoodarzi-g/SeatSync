using Moq;
using SeatSync.Core.DTOs;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;
using SeatSync.Core.Services;
using Xunit;

namespace SeatSync.Tests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<ISeatRepository> _seatRepository = new();
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _eventService = new EventService(_eventRepository.Object, _seatRepository.Object);
    }

    [Fact]
    public async Task CreateEventAsync_CreatesEventAndAllRequestedSeats()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        List<Seat>? capturedSeats = null;

        _eventRepository
            .Setup(r => r.CreateAsync(It.IsAny<Event>()))
            .ReturnsAsync((Event e) => e);

        _seatRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Seat>>()))
            .Callback<IEnumerable<Seat>>(seats => capturedSeats = seats.ToList())
            .Returns(Task.CompletedTask);

        var request = new CreateEventRequest(
            "Fall Jazz Night",
            "Blue Note Hall",
            new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc),
            new List<CreateSeatRequest>
            {
                new("A", 1, 45.00m),
                new("A", 2, 45.00m),
                new("B", 1, 35.00m)
            }
        );

        // Act
        var result = await _eventService.CreateEventAsync(organizerId, request);

        // Assert
        Assert.Equal("Fall Jazz Night", result.Name);
        Assert.Equal(organizerId, result.OrganizerId);
        Assert.Equal(3, result.Seats.Count);
        Assert.All(result.Seats, s => Assert.Equal("Available", s.Status));

        Assert.NotNull(capturedSeats);
        Assert.Equal(3, capturedSeats!.Count);
        Assert.All(capturedSeats, s => Assert.Equal(SeatStatus.Available, s.Status));
    }

    [Fact]
    public async Task GetEventByIdAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Arrange
        _eventRepository
            .Setup(r => r.GetByIdWithSeatsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _eventService.GetEventByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllEventsAsync_MapsSeatCountIntoSummary()
    {
        // Arrange
        var events = new List<Event>
        {
            new()
            {
                Name = "Event One",
                Venue = "Venue One",
                Seats = new List<Seat> { new(), new(), new() } // 3 seats
            },
            new()
            {
                Name = "Event Two",
                Venue = "Venue Two",
                Seats = new List<Seat>() // 0 seats
            }
        };

        _eventRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(events);

        // Act
        var result = await _eventService.GetAllEventsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].TotalSeats);
        Assert.Equal(0, result[1].TotalSeats);
    }
}