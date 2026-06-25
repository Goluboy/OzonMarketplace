# ADR-014: Отказ от promtail на Portainer

| Метаданные | Значение |
|---|---|
| **Дата** | `2026-06-22` |
| **Статус** | `🔴 Отклонено` |
| **Авторы** | `@Goluboy` |
| **Рецензенты** | `@darksvoid ` |

---

## Контекст

В Portainer нет root прав и необходимо адаптироваться под docker swarm, нет depends_on с условиями. А promtail требует монтирование /var/run/docker.sock и доступ к логам на хосте.

## Решение

Отказ от promtail в пользу Fluent Bit + Docker Fluentd Logging Driver. Не требуются права.

Для сервисов без OTEL интеграции(Kafka, PostgreSQL, Redis...), Docker сам может перенаправить эти логи в Fluent Bit через Fluentd protocol
Для сервисов OTEL интеграциq (Keycloak) логи трейсинга уйдут в Jaeger, а остальное в Fluent Bit
Для .NET есть AddOpenTelemetry().AddOtlpExporter(), либо как сервисы без OTEL

## Последствия
### Позитивные
- Fluent Bit легковесный, в сравнении с Promtail 
- Fluent Bit работает не только с loki, возможность расширения
- Совместим с Docker Swarm

### Негативные
- Требуется явная настройка logging в каждом сервисе
- требуется открытие localhost порта для Docker Fluentd Driver

## Рассмотренные альтернативы
| Вариант | Почему отклонён |
|---|---|
| `Docker json-file driver + Loki Docker plugin` | `Loki Docker plugin требует установки на хост через root (docker plugin install grafana loki-docker-driver). Невозможно на shared-хостинге без админ-прав. Также создает tight coupling между Docker и Loki.` |
| `OpenTelemetry Collector` | `Требует изменений в коде всех приложений (переход на OTLP-экспорт логов). Не покрывает сторонние сервисы (Keycloak, Kafka, PostgreSQL, Redis, MinIO), у них нет встроенной OTLP-интеграции. ` |



## Ссылки
- Issue: [#89](https://github.com/Goluboy/OzonMarketplace/issues/89)
- Документация/Спецификация: [docs/logging.md](/docs/logging.md)
