using Moq;
using SeatSync.Core.Interfaces;
using SeatSync.Core.Services;
using Xunit;

namespace SeatSync.Tests.Services;

public class HoldServiceTests
{
    private readonly Mock<ISeatLockService> _seatLockService = new();
    private readonly Mock<ISeatMapNotifier> _seatMapNotifier = new();
    private readonly HoldService _holdService;

    public HoldServiceTests()
    {
        _holdService = new HoldService(_seatLockService.Object, _seatMapNotifier.Object);
    }

    [Fact]
    public async Task HoldSeatAsync_WhenLockAcquired_ReturnsResponseAndNotifies()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _seatLockService
            .Setup(s => s.TryAcquireHoldAsync(eventId, seatId, userId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        // Act
        var result = await _holdService.HoldSeatAsync(eventId, seatId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seatId, result!.SeatId);
        Assert.True(result.HeldUntil > DateTime.UtcNow);

        _seatMapNotifier.Verify(
            n => n.NotifySeatStatusChangedAsync(eventId, seatId, "Held"),
            Times.Once);
    }

    [Fact]
    public async Task HoldSeatAsync_WhenLockNotAcquired_ReturnsNullAndDoesNotNotify()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _seatLockService
            .Setup(s => s.TryAcquireHoldAsync(eventId, seatId, userId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);

        // Act
        var result = await _holdService.HoldSeatAsync(eventId, seatId, userId);

        // Assert
        Assert.Null(result);

        // Important: no broadcast should fire for a failed hold attempt —
        // nothing actually changed, so notifying would be misleading.
        _seatMapNotifier.Verify(
            n => n.NotifySeatStatusChangedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ReleaseSeatAsync_WhenCalledByOwner_ReleasesAndNotifies()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _seatLockService
            .Setup(s => s.GetHoldOwnerAsync(eventId, seatId))
            .ReturnsAsync(userId);

        // Act
        await _holdService.ReleaseSeatAsync(eventId, seatId, userId);

        // Assert
        _seatLockService.Verify(s => s.ReleaseHoldAsync(eventId, seatId), Times.Once);
        _seatMapNotifier.Verify(n => n.NotifySeatStatusChangedAsync(eventId, seatId, "Available"), Times.Once);
    }

    [Fact]
    public async Task ReleaseSeatAsync_WhenCalledByNonOwner_DoesNothing()
    {
        // Arrange — this is the security-relevant case: user B trying to
        // release user A's active hold.
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var actualOwnerId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();

        _seatLockService
            .Setup(s => s.GetHoldOwnerAsync(eventId, seatId))
            .ReturnsAsync(actualOwnerId);

        // Act
        await _holdService.ReleaseSeatAsync(eventId, seatId, requestingUserId);

        // Assert
        _seatLockService.Verify(s => s.ReleaseHoldAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        _seatMapNotifier.Verify(
            n => n.NotifySeatStatusChangedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }
}