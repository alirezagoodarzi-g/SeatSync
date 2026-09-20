using SeatSync.Core.Interfaces;
using StackExchange.Redis;

namespace SeatSync.Infrastructure.Redis;

public class RedisSeatLockService : ISeatLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisSeatLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> TryAcquireHoldAsync(Guid eventId, Guid seatId, Guid userId, TimeSpan holdDuration)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(eventId, seatId);

        // SET key value NX EX <seconds> — atomic acquire-with-expiry.
        // StringSetAsync's "when: NotExists" maps to Redis's NX flag.
        return await db.StringSetAsync(
            key,
            userId.ToString(),
            expiry: holdDuration,
            when: When.NotExists
        );
    }

    public async Task ReleaseHoldAsync(Guid eventId, Guid seatId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(BuildKey(eventId, seatId));
    }

    public async Task<Guid?> GetHoldOwnerAsync(Guid eventId, Guid seatId)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(BuildKey(eventId, seatId));

        if (value.IsNullOrEmpty) return null;
        return Guid.TryParse(value.ToString(), out var userId) ? userId : null;
    }

    private static string BuildKey(Guid eventId, Guid seatId) => $"seat:{eventId}:{seatId}";
}