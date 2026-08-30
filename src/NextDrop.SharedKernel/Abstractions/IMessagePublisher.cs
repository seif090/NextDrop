namespace NextDrop.SharedKernel.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string exchange, string routingKey, CancellationToken cancellationToken = default) where T : class;
}
