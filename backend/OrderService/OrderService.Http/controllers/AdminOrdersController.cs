using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Domain.ValueObjects;
using OrderService.Http.Dtos;
using OrderService.Http.Dtos.Requests;
using OrderService.Http.Dtos.Shared;
using OrderService.Http.Extensions;
using OrderService.Http.Mappings;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;
using OrderService.UseCases.Queries;
using OrderService.UseCases.Queries.Handlers;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;
using OrderService.UseCases.Queries.Queries;

namespace OrderService.Http.controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/orders")]
public class AdminOrdersController(
    ICommandHandler<UpdateOrderStatusCommand, bool> updateOrderStatusHandler,
    ICommandHandler<ForceCancelOrderCommand, bool> forceCancelOrderHandler,
    IQueryHandler<GetOrderByIdQuery, OrderModel?> getOrderByIdHandler,
    IQueryHandler<GetAllOrdersQuery, OrderModel[]> getAllOrdersQueryHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AdminOrderPagedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminOrderPagedResult>> GetAllOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new GetAllOrdersQuery(
            page,
            pageSize,
            status == null ? null : (Domain.ValueObjects.OrderStatus)status,
            customerId,
            dateFrom,
            dateTo);

        var ordersPaged = await getAllOrdersQueryHandler.HandleAsync(query, cancellationToken);
        
        return Ok(ordersPaged);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDto>> GetOrderDetailsForAdmin(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order.ToAdminDto());
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDto>> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await updateOrderStatusHandler.HandleAsync(
                new UpdateOrderStatusCommand(id, (Domain.ValueObjects.OrderStatus)request.NewStatus, User.GetUserId(), request.Comment),
                cancellationToken);

            // Re-fetch the updated order to return
            var updatedOrder = await getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);
            if (updatedOrder is null)
            {
                return NotFound(); // Should not happen if update was successful
            }
            return Ok(updatedOrder.ToAdminDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(HttpContext.CreateProblemDetails("Status update failed", ex.Message));
        }
    }

    [HttpPost("{id:guid}/force-cancel")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOrderDto>> ForceCancelOrder(
        Guid id,
        [FromBody] ForceCancelOrderRequest request, // Assuming a DTO for force cancel reason
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Assuming ForceCancelOrderCommand exists and takes order ID and reason
            await forceCancelOrderHandler.HandleAsync(new ForceCancelOrderCommand(id, request.Reason), cancellationToken);

            // Re-fetch the updated order to return
            var cancelledOrder = await getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);
            if (cancelledOrder is null)
            {
                return NotFound(); // Should not happen if cancellation was successful
            }
            return Ok(cancelledOrder.ToAdminDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(HttpContext.CreateProblemDetails("Force cancellation failed", ex.Message));
        }
    }
}
