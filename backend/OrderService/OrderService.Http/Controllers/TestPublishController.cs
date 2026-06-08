using IntegrationEvents.IntegrationEvents;
using IntegrationEvents.Shared;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/test")]
public class TestPublishController : ControllerBase
{
    private readonly ITopicProducer<Guid, OrderCreatedEvent> _producer;

    public TestPublishController(ITopicProducer<Guid, OrderCreatedEvent> producer) =>
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

        await _producer.Produce(evt.CorrelationId, evt);
        return Ok(new { Status = "Sent", EventId = evt.EventId });
    }
}