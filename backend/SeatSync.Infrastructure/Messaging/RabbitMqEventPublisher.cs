using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SeatSync.Core.Interfaces;

namespace SeatSync.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    private async Task EnsureConnectedAsync()
    {
        if (_channel is not null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_channel is not null) return; // double-checked after acquiring lock

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishAsync<TMessage>(string queueName, TMessage message) where TMessage : class
    {
        await EnsureConnectedAsync();

        // Durable queue: survives a RabbitMQ restart. Matches "booking
        // confirmations must not be silently lost" — this queue holds
        // real user intent (they clicked confirm), not disposable data.
        await _channel!.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true // message itself survives a broker restart, not just the queue
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty, // default exchange — routes by queue name directly
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}