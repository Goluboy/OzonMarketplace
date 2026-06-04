# SAGA Choreography Documentation

Поток выполнения CreateProduct см. в docs/Sequence diagrams/Create Product.md

# 1 топик на домен/сервис

Все события, связанные с созданием заказа (OrderCreated, StockReserved, StockReservationFailed, PriceCalculated, OrderCancelled), публикуются в единый топик orders.
Решение см. в [ADR](/docs/ADR/006-kafka-topics-management.md)

## Контракты

| Ивент                         | 📤 Издатель      | 📥 Подписчик                         | 🗂️ Топик Kafka              |
| ----------------------------- | ---------------- | ------------------------------------ | ---------------------------- |
| `OrderCreatedEvent`           | `OrderService`   | `ProductService`, `PriceService`     | `orders`    |
| `StockReservedEvent`          | `ProductService` | `OrderService`                       | `orders` |
| `StockReservationFailedEvent` | `ProductService` | `OrderService`                       | `orders`   |
| `PriceCalculatedEvent`        | `PriceService`   | `OrderService`                       | `orders`       |
| `OrderCancelledEvent`         | `OrderService`   | `ProductService` (+ будущие сервисы) | `orders`        |

Интеграционные ивенты реализуются в каждом сервисе независимо.

```c#
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; } // = OrderId для связки шагов SAGA
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

## Изменение контракта события

### План миграции

1. **Добавить код V2**: Развернуть новый `OrderCreatedEventV2` и `OrderCreatedV2Consumer` в **ProductService**.
2. **Настроить слушателя**: Убедиться, что `TopicEndpoint` для V2 в ProductService активен.
3. **Деплой ProductService**: Сервис начнет слушать `orders` на наличие сообщений типа `OrderCreatedEventV2`.
4. **Обновить Publisher (OrderService)**:
    - В коде `CreateOrderHandler` заменить публикацию V1 на публикацию V2.
    - Топик публикации **не меняется** (`orders`). Меняется только тип сообщения.
5. **Деплой OrderService**: Сервис начинает писать V2 в топик `orders`.
6. **Результат**:
    - ProductService (V2 Consumer) берет сообщения и обрабатывает.
    - Если есть другие старые сервисы, они продолжат читать V1 (пока мы не отключим публикацию V1).
7. **Очистка**: Через релиз удалить V1 классы и слушателей.


### Таблица маршрутизации

| Версия | Класс C#              | Топик Kafka | Кто слушает                        |
| ------ | --------------------- | ----------- | ---------------------------------- |
| **V1** | `OrderCreatedEvent`   | `orders`    | Legacy-сервисы (ProductService v1) |
| **V2** | `OrderCreatedEventV2` | `orders`    | Новые сервисы (ProductService v2)  |

### Настройка Consumer

// Program.cs (ProductService)

x.UsingKafka((context, k) =>
{
    k.Host("kafka:9092");

    // 1. Подписка на V1 (в топик orders)
    k.TopicEndpoint<OrderCreatedEvent>("orders", "product-service-legacy", e =>
    {
        e.ConfigureConsumer<OrderCreatedConsumer>(context);
    });

    // 2. Подписка на V2 (ТОЖЕ в топик orders)
    // MassTransit использует заголовок MT-MessageType, чтобы отличить V2 от V1
    k.TopicEndpoint<OrderCreatedEventV2>("orders", "product-service-v2", e =>
    {
        e.ConfigureConsumer<OrderCreatedV2Consumer>(context);
    });
});

### Переход на новые версии

```
# Этап 1: Запуск
orders.order-created  # V1

# Этап 2: Добавили поле CustomerPhone (breaking change)
orders.order-created      # V1 (для старых сервисов)
orders.order-created.v2   # V2 (для новых сервисов)

# Этап 3: Все сервисы перешли на V2
orders.order-created.v2   # Основной топик
# orders.order-created удалён

# Этап 4: Новая версия
orders.order-created.v2   # Legacy
orders.order-created.v3   # Новая версия
```

# Ссылки

[ADR с решением перейти на хореографию](docs/ADR/003-saga.md)