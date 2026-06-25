# SAGA Choreography Documentation

Документация описывает реализацию хореографической SAGA для процесса создания заказа в маркетплейсе с использованием **DotNetCore.CAP 10.x** и Apache Kafka.

> **Историческая справка:** Изначально SAGA была реализована на MassTransit с оркестрацией. Миграция на DotNetCore.CAP + чистую хореографию описана в [ADR-006](/docs/ADR/006-kafka-topics-management.md).

---

## Архитектурные принципы

### 1 топик на агрегат (Domain)

Вместо классического "1 топик = 1 событие" используется подход **"1 топик на агрегат"**, где внутри одного топика маршрутизируются все события, связанные с одной доменной сущностью. Это гарантирует:

- ✅ Строгий порядок событий внутри агрегата (важно для SAGA)
- ✅ Меньше топиков в Kafka (проще администрирование)
- ✅ Единая партиция на OrderId (все события одного заказа обрабатываются последовательно)

### Топики системы

| Топик | Агрегат | Publisher | Основные события |
|-------|---------|-----------|------------------|
| `orders` | Order | `OrderService` | `OrderCreatedEvent`, `OrderTimeoutEvent`, `OrderCancelledEvent` |
| `products` | Product/Stock | `ProductService` | `StockReservedEvent`, `StockReservationFailedEvent` |
| `prices` | Price | `PriceService` | `PriceCalculatedEvent` |

### Dispatcher Pattern

Вместо прямой подписки Consumer'ов на топик используется **Dispatcher**, который принимает базовый `IntegrationEvent` и маршрутизирует события по типу:

```csharp
[CapSubscribe("products")]
public async Task HandleAsync(IntegrationEvent @event, CapHeader header, CancellationToken ct)
{
    switch (@event)
    {
        case StockReservedEvent e:
            await stockReservedConsumer.HandleAsync(e, header, ct);
            break;
        case StockReservationFailedEvent e:
            await stockFailedConsumer.HandleAsync(e, header, ct);
            break;
    }
}
```

Полиморфная десериализация обеспечивается атрибутом `[JsonDerivedType]` на базовом классе `IntegrationEvent`.

---

## Контракты событий

### Базовый класс

```csharp
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; }  // = OrderId для связки шагов SAGA
    public Guid OrderId => CorrelationId;
}
```

### Таблица событий

| Ивент | 📤 Издатель | 📥 Подписчики | Топик |
|-------|-------------|---------------|-------|
| `OrderCreatedEvent` | `OrderService` | `ProductService`, `PriceService`, `OrderService` (timeout) | `orders` |
| `OrderTimeoutEvent` | `OrderService` (delayed) | `OrderService` | `orders` |
| `StockReservedEvent` | `ProductService` | `OrderService` | `products` |
| `StockReservationFailedEvent` | `ProductService` | `OrderService` | `products` |
| `PriceCalculatedEvent` | `PriceService` | `OrderService` | `prices` |
| `OrderCancelledEvent` | `OrderService` | `ProductService` (+ будущие) | `orders` |


---

## Поток выполнения SAGA

Полный sequence diagram см. в [docs/Sequence diagrams/Create Order SAGA.md](/docs/Sequence%20diagrams/Create%20Product.md).

---

## Надёжность и отказоустойчивость

| Механизм | Реализация в DotNetCore.CAP |
|----------|-----------------------------|
| **Outbox Pattern** | Атомарная запись бизнес-изменений и `cap.published` через `BeginOutboxTransactionAsync()` и `CommitAsync()`. CAP worker отправляет в Kafka только после commit. |
| **Идемпотентность** | Таблица `processed_events (message_id TEXT PRIMARY KEY)`. Consumer проверяет через `IsProcessedAsync()` перед выполнением, пишет через `MarkAsProcessedAsync()` в той же транзакции, что и бизнес-логика. |
| **Таймаут SAGA** | `capPublisher.PublishDelayAsync(TimeSpan.FromMinutes(15), "orders", new OrderTimeoutEvent(...))`. CAP сохраняет отложенное сообщение в `cap.published` с задержкой и отправляет по расписанию. **Scheduler не нужен.** |
| **Distributed Tracing** | `SagaCorrelationFilter` + `CorrelatedCapPublisher` обеспечивают проброс `cap-corr-id = OrderId` через все события SAGA. В Grafana Tempo видно всю цепочку по `OrderId`. |
| **Компенсация** | `OrderCancelledEvent` публикуется только из `OrderService` (из `ForceCancelOrderCommandHandler`). ProductService подписан и снимает резерв стока идемпотентно. |
| **Повторные попытки** | Встроенный CAP retry: 3 попытки с экспоненциальной задержкой, затем сообщение помечается как `Failed` и требует ручного вмешательства. |
| **Управление состоянием** | Состояние хранится в самом агрегате `Order` (статус, `IsReserved`, `PaidAt`). Отдельная таблица `order_saga_state` не нужна благодаря Outbox. |
| **Версионирование** | `IVersioned` в агрегате + `row_version` в БД для защиты от lost update при конкурентных изменениях. |

### Как обеспечивается атомарность

```csharp
// Handler (UseCase)
await unitOfWork.BeginOutboxTransactionAsync(ct);  // CAP транзакция
try
{
    order.MarkItemsAsReserved(reservedItems);
    await orderRepository.SaveAsync(order, ct);
    await capPublisher.PublishAsync(...);
    await processedEvents.MarkAsProcessedAsync(...);
    await unitOfWork.CommitAsync(ct);
}
catch { await unitOfWork.RollbackAsync(ct); throw; }
```

---

## Distributed Tracing

Все события в рамках одной SAGA имеют одинаковый `cap-corr-id`, равный `OrderId`. Это достигается через:

1. **На старте SAGA** (в `CreateOrderCommandHandler`): `cap-corr-id` задается явно через headers.
2. **В Consumer'ах**: `SagaCorrelationFilter` читает `cap-corr-id` из Kafka Headers и сохраняет в `AsyncLocal`.
3. **При последующих публикациях**: `CorrelatedCapPublisher` (декоратор `ICapPublisher`) автоматически добавляет `cap-corr-id` из `AsyncLocal` в заголовки новых сообщений.

### Поиск трейсов

В Grafana Tempo / Loki:
```logql
{service=~"OrderService|ProductService|PriceService"} 
  | json | CorrelationId="ce055aa6-..."
```

Отобразит всю цепочку: от создания заказа до отмены или успешного завершения.

---

## Миграция контрактов событий

В отличие от MassTransit, DotNetCore.CAP не имеет встроенной версионизации через `MT-MessageType`. Используется **явная маршрутизация по `cap-msg-type`** header'у.

### План миграции (пример: добавление `CustomerPhone` в `OrderCreatedEvent`)

1. **Создать V2 контракт в `IntegrationEvents` либе:**
   ```csharp
   public record OrderCreatedEventV2 : IntegrationEvent
   {
       public string CustomerPhone { get; init; }  // Новое поле
       // ... остальные поля
   }
   ```
   Добавить `[JsonDerivedType(typeof(OrderCreatedEventV2), nameof(OrderCreatedEventV2))]`.

2. **Обновить Dispatcher в ProductService:**
   ```csharp
   switch (@event)
   {
       case OrderCreatedEventV2 v2:
           await consumer.HandleAsync(v2, header, ct);  // Новая логика
           break;
       case OrderCreatedEvent v1:
           await consumer.HandleAsync(v1, header, ct);  // Старая логика (fallback)
           break;
   }
   ```

3. **Деплой ProductService** (потребитель): начинает принимать оба типа.

4. **Обновить Publisher в OrderService:** публиковать `OrderCreatedEventV2`.

5. **Деплой OrderService** (издатель): пишет только V2 в топик `orders`.

6. **Через релиз:** удалить V1 Consumer из ProductService и старый класс `OrderCreatedEvent`.

### Таблица маршрутизации версий

| Версия | Класс | Топик | Кто слушает |
|--------|-------|-------|-------------|
| V1 | `OrderCreatedEvent` | `orders` | Legacy-сервисы |
| V2 | `OrderCreatedEventV2` | `orders` | Новые сервисы |

### Жизненный цикл топика при миграции

```
# Этап 1: Запуск
orders (V1 events)

# Этап 2: Breaking change (CustomerPhone)
orders (V1 + V2 events, маршрутизация по cap-msg-type header)

# Этап 3: Все сервисы на V2
orders (только V2 events)

# Этап 4: Новая миграция
orders (V2 + V3 events)
```

**Ключевое отличие от MassTransit:** CAP использует заголовок `cap-msg-type` для идентификации типа события, а не отдельный routing key в топике. Это позволяет держать **один топик на агрегат** даже при наличии нескольких версий контрактов.

---

## Компоненты OrderService

### Проекты

```
OrderService/
├── Domain/                     ← Entities, Value Objects, Domain Events
├── UseCases.Commands/          ← Handlers (оркестрация, без бизнес-логики)
├── UseCases.Queries/           ← Read model
├── Infrastructure.Persistence/ ← Dapper, UnitOfWork, Migrations
├── Infrastructure.EventBus/    ← Consumers, Dispatchers, Tracing
└── Http/                       ← Controllers
```

### Consumers в `Infrastructure.EventBus`

| Consumer | Обрабатывает | Вызывает Handler |
|----------|--------------|------------------|
| `OrderCreatedConsumer` | `OrderCreatedEvent` | — (или публикует таймаут) |
| `OrderSagaTimeoutConsumer` | `OrderTimeoutEvent` | `ForceCancelOrderCommand` |
| `OrderCancelledConsumer` | `OrderCancelledEvent` | — |
| `StockReservedConsumer` | `StockReservedEvent` | `UpdateOrderStockStatusCommand` |
| `StockReservationFailedConsumer` | `StockReservationFailedEvent` | `ForceCancelOrderCommand` |
| `PriceCalculatedConsumer` | `PriceCalculatedEvent` | `UpdateOrderPriceCommand` |

### Dispatchers

- `OrdersEventDispatcher` → топик `orders`
- `ProductsEventDispatcher` → топик `products`
- `PricesEventDispatcher` → топик `prices`

---

## 📚 Ссылки

- [ADR-003: Решение перейти на хореографию](docs/ADR/003-saga.md)
- [ADR-003: Outbox Transactional](docs/ADR/004-outbox.md)
- [ADR-006: Стратегия управления топиками Kafka (1 топик на 1 ивент))](docs/ADR/006-kafka-topics-management.md)
- [ADR-007: Стратегия управления топиками Kafka (1 топик на бизнес-флоу](docs/ADR/007-kafka-topics-management2.md)