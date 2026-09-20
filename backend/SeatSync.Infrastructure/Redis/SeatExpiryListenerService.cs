using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeatSync.Core.Interfaces;
using StackExchange.Redis;

namespace SeatSync.Infrastructure.Redis;

public partial class SeatExpiryListenerService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeatExpiryListenerService> _logger;

    public SeatExpiryListenerService(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<SeatExpiryListenerService> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Database 0's expired-key channel. StackExchange.Redis subscribes
        // to this the same way it would any other pub/sub channel.
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal("__keyevent@0__:expired"),
            async (_, keyValue) => await HandleExpiredKeyAsync(keyValue.ToString(), stoppingToken)
        );

        _logger.LogInformation("Subscribed to Redis expired-key notifications.");

        // Keep the background service alive until shutdown.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task HandleExpiredKeyAsync(string expiredKey, CancellationToken stoppingToken)
    {
        // Only care about seat hold keys — ignore anything else that
        // might expire in this Redis instance later (e.g. future rate-limit keys).
        var match = SeatKeyPattern().Match(expiredKey);
        if (!match.Success) return;

        var eventId = Guid.Parse(match.Groups["eventId"].Value);
        var seatId = Guid.Parse(match.Groups["seatId"].Value);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<ISeatMapNotifier>();

            await notifier.NotifySeatStatusChangedAsync(eventId, seatId, "Available");

            _logger.LogInformation(
                "Hold expired naturally for seat {SeatId} on event {EventId}, broadcasted Available.",
                seatId, eventId);
        }
        catch (Exception ex)
        {
            // Deliberately just log, not throw — a missed broadcast for one
            // expired hold shouldn't crash this listener for every future
            // expiry. Worst case, that one seat's UI is stale until the
            // next state-changing action on it.
            _logger.LogError(ex, "Failed to broadcast expiry for key {Key}", expiredKey);
        }
    }

    [GeneratedRegex(@"^seat:(?<eventId>[0-9a-fA-F-]{36}):(?<seatId>[0-9a-fA-F-]{36})$")]
    private static partial Regex SeatKeyPattern();
}