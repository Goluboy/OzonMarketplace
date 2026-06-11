using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Domain.ValueObjects;
using OrderService.Http.Dtos;
using OrderService.Http.Extensions;
using OrderService.Http.Mappings;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;
using OrderService.UseCases.Queries.Queries;

namespace OrderService.Http.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController(
    ICommandHandler<CreateOrderCommand, Guid> createOrderHandler,
    ICommandHandler<CancelOrderCommand, bool> cancelOrderHandler,
    ICommandHandler<UpdateOrderStatusCommand, bool> updateOrderStatusHandler,
    IQueryHandler<GetOrderByIdQuery, OrderModel?> getOrderByIdHandler,
    IQueryHandler<GetOrdersByCustomerIdQuery, List<OrderModel>> getOrdersByCustomerIdHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(OrderPagedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrderPagedResult>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        var customerId = User.GetUserId();

        var orders = await getOrdersByCustomerIdHandler.HandleAsync(
            new GetOrdersByCustomerIdQuery(customerId),
            cancellationToken);

        if (status is not null)
        {
            orders = orders.Where(o => o.Status == status).ToList();
        }

        var totalCount = orders.Count;
        var pageItems = orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => o.ToDto())
            .ToList();

        return Ok(new OrderPagedResult(pageItems, totalCount, page, pageSize));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateOrderAcceptedResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerId = User.GetUserId();
        var userEmail = User.GetEmail();
        var userName = User.GetName();

        if (request.Items is null || request.Items.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Items), "At least one order item is required");
            return ValidationProblem(ModelState);
        }

        var customerEmail = !string.IsNullOrWhiteSpace(request.CustomerEmail)
            ? request.CustomerEmail
            : userEmail;

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            ModelState.AddModelError(
                nameof(request.CustomerEmail),
                "customerEmail is required either in request body or in user profile");
            return ValidationProblem(ModelState);
        }

        var customerName = !string.IsNullOrWhiteSpace(request.CustomerName)
            ? request.CustomerName
            : userName ?? "Customer";

        var command = new CreateOrderCommand(
            customerId,
            customerName,
            customerEmail,
            request.DeliveryAddress,
            request.Items.Select(item => new CreateOrderCommand.CreateOrderItemCommand(
                item.ProductId,
                $"Product-{item.ProductId}",
                item.Quantity,
                0m)).ToList());

        try
        {
            var orderId = await createOrderHandler.HandleAsync(command, cancellationToken);

            return Accepted(new CreateOrderAcceptedResponse(
                orderId,
                OrderStatus.Created,
                $"/api/orders/{orderId}/status"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(HttpContext.CreateProblemDetails(
                "Order cannot be created",
                ex.Message));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (!User.IsAdmin() && order.CustomerId != User.GetUserId())
        {
            return Forbid();
        }

        return Ok(order.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var cancelled = await cancelOrderHandler.HandleAsync(
                new CancelOrderCommand(id, User.GetUserId()),
                cancellationToken);

            if (!cancelled)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(HttpContext.CreateProblemDetails(
                "Cannot cancel order",
                ex.Message));
        }
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderStatusCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderStatusCheckResponse>> GetOrderStatus(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (!User.IsAdmin() && order.CustomerId != User.GetUserId())
        {
            return Forbid();
        }

        return Ok(new OrderStatusCheckResponse(
            order.Status,
            GetStatusMessage(order.Status),
            order.UpdatedAt ?? order.CreatedAt));
    }

    private static string? GetStatusMessage(OrderStatus status) =>
        status switch
        {
            OrderStatus.Created => "Waiting for stock reservation...",
            OrderStatus.Paid => "Payment confirmed.",
            OrderStatus.Assembling => "Order is being assembled.",
            OrderStatus.Shipping => "Order is on the way.",
            OrderStatus.Delivered => "Order delivered.",
            OrderStatus.Cancelled => "Order cancelled.",
            _ => null
        };
}
