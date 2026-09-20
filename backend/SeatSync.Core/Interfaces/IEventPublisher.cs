namespace SeatSync.Core.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TMessage>(string queueName, TMessage message) where TMessage : class;
}