using SeatSync.Infrastructure.Redis;
using StackExchange.Redis;
using Xunit;

namespace SeatSync.Tests.Services;

public class SeatLockConcurrencyTests : IAsyncLifetime
{
    private IConnectionMultiplexer _redis = null!;
    private RedisSeatLockService _lockService = null!;

    public async Task InitializeAsync()
    {
        // Requires the seatsync-redis container from docker-compose to be running.
        _redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
        _lockService = new RedisSeatLockService(_redis);
    }

    public Task DisposeAsync()
    {
        _redis.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task TryAcquireHoldAsync_WithManyConcurrentRequestsForSameSeat_OnlyOneSucceeds()
    {
        // Arrange — this is the exact scenario from the spec: many users
        // clicking the same seat within milliseconds of each other.
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        const int concurrentUsers = 50;

        var userIds = Enumerable.Range(0, concurrentUsers)
            .Select(_ => Guid.NewGuid())
            .ToList();

        // Act — fire all requests at genuinely the same time. Task.WhenAll
        // starts every task before awaiting any of them, and StackExchange.Redis
        // multiplexes them over the same connection concurrently, so this is a
        // real race against Redis, not a simulated one.
        var tasks = userIds.Select(userId =>
            _lockService.TryAcquireHoldAsync(eventId, seatId, userId, TimeSpan.FromMinutes(5))
        );

        var results = await Task.WhenAll(tasks);

        // Assert — exactly one request won the lock, all 49 others were
        // correctly rejected. This is the property that prevents double-booking.
        var successCount = results.Count(acquired => acquired);
        Assert.Equal(1, successCount);

        // Confirm the lock is actually held by one of our users (not lost/corrupted)
        var owner = await _lockService.GetHoldOwnerAsync(eventId, seatId);
        Assert.NotNull(owner);
        Assert.Contains(owner!.Value, userIds);

        // Cleanup
        await _lockService.ReleaseHoldAsync(eventId, seatId);
    }

    [Fact]
    public async Task TryAcquireHoldAsync_ForDifferentSeats_AllSucceedIndependently()
    {
        // Sanity check in the other direction — concurrency control should be
        // per-seat, not accidentally global. 10 different users holding 10
        // different seats simultaneously should all succeed.
        var eventId = Guid.NewGuid();
        var seatIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

        var tasks = seatIds.Select(seatId =>
            _lockService.TryAcquireHoldAsync(eventId, seatId, Guid.NewGuid(), TimeSpan.FromMinutes(5))
        );

        var results = await Task.WhenAll(tasks);

        Assert.All(results, acquired => Assert.True(acquired));

        // Cleanup
        foreach (var seatId in seatIds)
        {
            await _lockService.ReleaseHoldAsync(eventId, seatId);
        }
    }
}