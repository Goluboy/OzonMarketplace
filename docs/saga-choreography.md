# SAGA Choreography Documentation

Поток выполнения CreateProduct см. в docs/Sequence diagrams/Create Product.md

## Контракты

| Ивент                         | 📤 Издатель      | 📥 Подписчик                         | 🗂️ Топик Kafka              |
| ----------------------------- | ---------------- | ------------------------------------ | ---------------------------- |
| `OrderCreatedEvent`           | `OrderService`   | `ProductService`, `PriceService`     | `orders.order-created.v1`    |
| `StockReservedEvent`          | `ProductService` | `OrderService`                       | `products.stock-reserved.v1` |
| `StockReservationFailedEvent` | `ProductService` | `OrderService`                       | `products.stock-failed.v1`   |
| `PriceCalculatedEvent`        | `PriceService`   | `OrderService`                       | `prices.calculated.v1`       |
| `OrderCancelledEvent`         | `OrderService`   | `ProductService` (+ будущие сервисы) | `orders.cancelled.v1`        |

Интеграционные ивенты реализуются в каждом сервисе независимо.

```c#
public abstract record IntegrationEvent : DomainEvent
{
    public Guid CorrelationId { get; init; } // = OrderId
}

public record OrderCreatedEvent : IntegrationEvent
{
    public List<OrderItemDto> Items { get; init; } = new();
    public string CustomerEmail { get; init; } = string.Empty;
    public string DeliveryAddress { get; init; } = string.Empty;
}

public record StockReservedEvent : IntegrationEvent
{
    public List<Guid> ReservedProductIds { get; init; } = new();
}

public record StockReservationFailedEvent : IntegrationEvent
{
    public string Reason { get; init; } = string.Empty;
    public List<Guid> FailedProductIds { get; init; } = new();
}

public record PriceCalculatedEvent : IntegrationEvent
{
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "RUB";
}

public record OrderCancelledEvent : IntegrationEvent
{
    public string Reason { get; init; } = string.Empty;
    public List<Guid> ItemsToRelease { get; init; } = new();
}

public record OrderItemDto(Guid ProductId, int Quantity);
```


## Надёжность и отказоустойчивость
| Механизм                  | Реализация                                                                                                                 |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **Идемпотентность**       | Таблица `processed_events (event_id UUID PRIMARY KEY)`. Проверка `INSERT ... ON CONFLICT DO NOTHING` перед бизнес-логикой. |
| **Управление состоянием** | Таблица `order_saga_state` с полем `row_version`. Обновление через `WHERE order_id = @id AND row_version = @ver`.          |
| **Таймаут**               | MassTransit Scheduler публикует `SagaTimeoutEvent` через 30с. При получении - автоматическая отмена.                       |
| **Компенсация**           | `OrderCancelledEvent` публикуется только из `OrderService`. Подписчики обрабатывают его идемпотентно.                      |
| **Повторные попытки**     | MassTransit retry: `3 attempts × 5s`, затем Dead Letter Queue.                                                             |

### Изменение контракта события

1. Создайте EventV2 (например, OrderCreatedEventV2).
2. Опубликуйте в новый топик orders.order-created.v2.
3. Сохраните обратную совместимость минимум 1 релиз.
4. Обновите потребителей постепенно.


# Ссылки

[ADR с решением перейти на хореографию](docs/ADR/003-saga.md)