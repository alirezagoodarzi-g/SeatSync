using Moq;
using SeatSync.Core.DTOs;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;
using SeatSync.Core.Services;
using Xunit;

namespace SeatSync.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<ISeatRepository> _seatRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IReceiptGenerator> _receiptGenerator = new();
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _bookingService = new BookingService(
            _eventPublisher.Object,
            _bookingRepository.Object,
            _eventRepository.Object,
            _seatRepository.Object,
            _userRepository.Object,
            _receiptGenerator.Object
        );
    }

    [Fact]
    public async Task RequestConfirmationAsync_PublishesMessage_AndReturnsPendingWithoutTouchingRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new ConfirmBookingRequest(eventId, seatIds);

        // Act
        var result = await _bookingService.RequestConfirmationAsync(userId, request);

        // Assert
        Assert.Equal("Pending", result.Status);
        Assert.NotEqual(Guid.Empty, result.BookingRequestId);

        _eventPublisher.Verify(
            p => p.PublishAsync(BookingService.ConfirmQueueName, It.IsAny<ConfirmBookingMessage>()),
            Times.Once);

        _bookingRepository.Verify(r => r.CreateAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task RequestConfirmationAsync_PublishedMessage_CarriesCorrectData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatIds = new List<Guid> { Guid.NewGuid() };
        var request = new ConfirmBookingRequest(eventId, seatIds);

        ConfirmBookingMessage? capturedMessage = null;
        _eventPublisher
            .Setup(p => p.PublishAsync(BookingService.ConfirmQueueName, It.IsAny<ConfirmBookingMessage>()))
            .Callback<string, ConfirmBookingMessage>((_, msg) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _bookingService.RequestConfirmationAsync(userId, request);

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.Equal(result.BookingRequestId, capturedMessage!.BookingRequestId);
        Assert.Equal(userId, capturedMessage.UserId);
        Assert.Equal(eventId, capturedMessage.EventId);
        Assert.Equal(seatIds, capturedMessage.SeatIds);
    }
}