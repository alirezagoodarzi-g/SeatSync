using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SeatSync.Core.DTOs;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;
using SeatSync.Core.Services;

namespace SeatSync.Infrastructure.Messaging;

public class BookingConsumerService : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingConsumerService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public BookingConsumerService(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<BookingConsumerService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        // Simple retry loop — RabbitMQ's container might not be ready yet
        // when this hosted service starts (e.g. during `docker compose up`
        // race with `dotnet run`), so don't just crash the whole API.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not connect to RabbitMQ, retrying in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        if (_channel is null) return; // cancellation requested before connecting

        await _channel.QueueDeclareAsync(
            queue: BookingService.ConfirmQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        // Process one message at a time per consumer instance — simplest
        // correct behavior for this project's scale, avoids needing to
        // think about concurrent DbContext usage within the consumer itself.
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) => await HandleMessageAsync(eventArgs, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: BookingService.ConfirmQueueName,
            autoAck: false, // manual ack — this is what lets us control redelivery on failure
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        // Keep the background service alive until the app shuts down.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken)
    {
        var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        try
        {
            var message = JsonSerializer.Deserialize<ConfirmBookingMessage>(json)
                ?? throw new InvalidOperationException("Deserialized message was null.");

            // New DI scope per message — see the note above on why this matters.
            using var scope = _scopeFactory.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var seatLockService = scope.ServiceProvider.GetRequiredService<ISeatLockService>();
            var seatRepository = scope.ServiceProvider.GetRequiredService<ISeatRepository>();
            var seatMapNotifier = scope.ServiceProvider.GetRequiredService<ISeatMapNotifier>();

            // --- Idempotency check: is this a redelivery of an already-processed message? ---
            if (await bookingRepository.ExistsByRequestIdAsync(message.BookingRequestId))
            {
                _logger.LogInformation(
                    "BookingRequestId {Id} already processed, skipping (redelivery).",
                    message.BookingRequestId);

                await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                return;
            }

            // --- Real work: persist the booking, mark seats booked, release holds ---
            var booking = new Booking
            {
                BookingRequestId = message.BookingRequestId,
                UserId = message.UserId,
                EventId = message.EventId,
                SeatIds = message.SeatIds,
                Status = BookingStatus.Confirmed,
                ConfirmedAt = DateTime.UtcNow
            };

            await bookingRepository.CreateAsync(booking);

            foreach (var seatId in message.SeatIds)
            {
                await seatRepository.UpdateStatusAsync(seatId, SeatStatus.Booked);
                await seatLockService.ReleaseHoldAsync(message.EventId, seatId);
                await seatMapNotifier.NotifySeatStatusChangedAsync(message.EventId, seatId, "Booked");
            }

            _logger.LogInformation(
                "Booking {BookingId} confirmed for user {UserId}, {SeatCount} seat(s).",
                booking.Id, booking.UserId, booking.SeatIds.Count);

            await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process booking confirmation message.");

            // requeue: true — put it back for retry rather than discarding it.
            // A transient failure (e.g. Postgres briefly unreachable) shouldn't
            // silently drop a user's booking confirmation.
            await _channel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
        }

    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}