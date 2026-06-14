# ADR-011: Переход на DotNetCore.CAP

| Метаданные | Значение |
|---|---|
| **Дата** | `2026-06-14` |
| **Статус** | `🟢 Принято` |
| **Авторы** | `@Goluboy` |
| **Затронутые компоненты** | `OrderService, ProductService, PriceService, Kafka` |

---

## Контекст

Изначально для работы с Kafka мы использовали MassTransit. В процессе реализации SAGA Choreography столкнулись с рядом критических ограничений.
Проблемы MassTransit + Kafka:
1. Один producer на топик: Rider не позволяет зарегистрировать несколько producer'ов на один топик напрямую. Это ломает архитектуру "1 топик на агрегат", где разные UseCases публикуют события независимо.
2. Нет Outbox Pattern из коробки, только для EF core.
3. Delayed Messages требуют Quartz: Для SAGA таймаутов нужен отдельный scheduler (Quartz) + доп. инфраструктура, хотя это должно быть нативной возможностью messaging-фреймворка.

## Решение

Переход на DotNetCore.CAP, он из коробки поддерживает необходимый функционал:
- Простая регистрания через ICapPublisher
- Встроенные таблицы cap.published / cap.received в PostgreSQL
- Отложенные сообщения на нативном PublishDelayAsync(TimeSpan) через Kafka headers 
- Идемпотентность через встроенный механизм через cap.received

## Последствия
### Позитивные
- Observability: Таблицы cap.published / cap.received дают полную видимость состояния очередей
- Проще инфраструктура: Убираем Quartz/Hangfire для delayed messages

### Негативные
-  Миграция: Переписать все publishers и consumers
- Меньше community: Меньше StackOverflow и документации

### Влияние на Frontend
- 

## Рассмотренные альтернативы

| Вариант | Почему отклонён |
|---|---|
| **MassTransit + Quartz + свой Outbox** | Слишком много ручной работы и проблем |
| **Чистый Confluent.Kafka** | Придется с нуля писать retry, DLQ, идемпотентность, мониторинг |
| **NServiceBus** | Коммерческая лицензия, overkill для нашего use case |


## Ссылки
- Issue: [#72](https://github.com/Goluboy/OzonMarketplace/issues/72)
- PR: [#69](https://github.com/Goluboy/OzonMarketplace/pull/69)
