using MassTransit;
using Microsoft.AspNetCore.Mvc;
using OrderService.IntegrationEvents;
using OrderService.IntegrationEvents.IntegrationEvents;
using OrderService.IntegrationEvents.Shared;

[ApiController]
[Route("api/test")]
public class TestPublishController : ControllerBase
{
    private readonly ITopicProducer<OrderCreatedEvent> _producer;

    public TestPublishController(ITopicProducer<OrderCreatedEvent> producer) =>
        _producer = producer;

    [HttpPost("kafka")]
    public async Task<IActionResult> SendTest()
    {
        var evt = new OrderCreatedEvent
        {
            CorrelationId = Guid.NewGuid(),
            CustomerEmail = "test@example.com",
            Items = new List<OrderItemDto> { new(Guid.NewGuid(), 1) }
        };

        await _producer.Produce(evt);
        return Ok(new { Status = "Sent", EventId = evt.EventId });
    }
}