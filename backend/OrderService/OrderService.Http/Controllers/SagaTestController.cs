using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Infrastructure.EventBus.Consumers;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;

namespace OrderService.Http.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SagaTestController(ICommandHandler<CreateOrderCommand, Guid> handler) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get() 
        {
            await handler.HandleAsync(new CreateOrderCommand(new Guid("123e4567-e89b-12d3-a456-426655440000"), "fds", "fsd@fds.com", null, [new CreateOrderCommand.CreateOrderItemCommand(new Guid("123e4567-e89b-12d3-a456-426655440000"), "dasdsa", 3, 12)]));
            return Ok();
        }
    }
}
