namespace OrderService.Application.Commands.Common;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message) where T : class;
}