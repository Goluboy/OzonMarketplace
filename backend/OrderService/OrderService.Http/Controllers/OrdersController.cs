using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Domain.ValueObjects;
using OrderService.Http.Dtos;
using OrderService.Http.Dtos.Requests;
using OrderService.Http.Dtos.Responses;
using OrderService.Http.Dtos.Shared;
using OrderService.Http.Extensions;
using OrderService.Http.Mappings;
using OrderService.UseCases.Commands.Commands;
using OrderService.UseCases.Commands.Interfaces;
using OrderService.UseCases.Queries;
using OrderService.UseCases.Queries.Handlers;
using OrderService.UseCases.Queries.Interfaces;
using OrderService.UseCases.Queries.Models;


namespace OrderService.Http.controllers;


[ApiController]
[Authorize(Policy = "CustomerOnly")]
[Route("api/orders")]
public class OrdersController(
    ICommandHandler<CreateOrderCommand, Guid> createOrderHandler,
    ICommandHandler<CancelOrderCommand, bool> cancelOrderHandler,
    IQueryHandler<GetOrderByIdQuery, OrderModel?> getOrderByIdHandler,
    IQueryHandler<GetOrdersByCustomerIdQuery, List<OrderModel>> getOrdersByCustomerIdHandler) : ControllerBase
{
    /// <summary>
    /// Получение списка заказов текущего пользователя
    /// </summary>
    /// <param name="page">Номер страницы (начинается с 1)</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    /// <param name="status">Фильтр по статусу заказа</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Список заказов</returns>
    [HttpGet]
    [ProducesResponseType(typeof(OrderPagedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OrderDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var customerId = User.GetUserId();

        var query = new GetOrdersByCustomerIdQuery(
            customerId,
            page,
            pageSize);

        var ordersPaged = await getOrdersByCustomerIdHandler.HandleAsync(
            query,
            cancellationToken);
        
        return Ok(ordersPaged);
    }

    /// <summary>
    /// Оформление нового заказа
    /// </summary>
    /// <remarks>
    /// Создает заказ на основе переданного списка товаров из корзины клиента.
    /// </remarks>
    /// <param name="request">Запрос на создание заказа</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Заказ принят в обработку</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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
                $"Product-{item.ProductId}", // Placeholder, actual product name should come from product service
                item.Quantity,
                0m)).ToList()); // Placeholder, actual price should come from product service

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

    /// <summary>
    /// Получение информации о заказе
    /// </summary>
    /// <param name="id">Идентификатор заказа</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Информация о заказе</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderByIdForCurrentUser(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customerId = User.GetUserId();
        var order = await getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        if (order.CustomerId != customerId)
        {
            return Forbid();
        }

        return Ok(order.ToDto());
    }

    /// <summary>
    /// Отмена заказа пользователем
    /// </summary>
    /// <remarks>
    /// Позволяет пользователю отменить свой заказ, если он ещё не перешёл в статус "Собирается".
    /// </remarks>
    /// <param name="id">Идентификатор заказа</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Заказ отменен</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customerId = User.GetUserId();
        var order = await getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        if (order.CustomerId != customerId)
        {
            return Forbid();
        }
        
        // Assuming CancelOrderCommand handles the business logic for cancellation rules
        try
        {
            await cancelOrderHandler.HandleAsync(new CancelOrderCommand(id, customerId), cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(HttpContext.CreateProblemDetails(
                "Order cancellation failed",
                ex.Message));
        }
    }

    /// <summary>
    /// Проверка статуса обработки заказа (для SAGA)
    /// </summary>
    /// <param name="id">Идентификатор заказа</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Текущий статус обработки</returns>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderStatusCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderStatusCheckResponse>> CheckOrderStatus(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customerId = User.GetUserId();
        var order = await getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        if (order.CustomerId != customerId)
        {
            return Forbid();
        }

        return Ok(new OrderStatusCheckResponse(
            order.Status,
            $"Order status is: {order.Status}",
            order.UpdatedAt ?? order.CreatedAt));
    }
}