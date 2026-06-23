# Обзор

| Ограничение                         | Влияние на логирование                                   |
| ----------------------------------- | -------------------------------------------------------- |
| **Отсутствие root-прав**            | Невозможно использовать Promtail (требует `docker.sock`) |
| **Portainer блокирует bind-mounts** | Нельзя монтировать `/var/log` или локальные конфиги      |
| **Docker Swarm**                    | ограничения, overlay-сети изолированы от хоста           |
# Архитектура

```mermaid
flowchart TB
    subgraph DockerSwarmStack["Docker Swarm Stack"]
        subgraph Services["Application Services"]
            Keycloak[Keycloak]
            Postgres[Postgres]
            Nginx[Nginx]
            Jaeger[Jaeger]
            ProductService[Product Service]
            OrderService[Order Service]
            Frontend[Frontend]
        end
        
        subgraph LoggingPipeline["Logging Pipeline"]
            FluentdDriver[Docker Fluentd<br/>Logging Driver]
            FluentBit[Fluent Bit<br/>log collector]
        end
        
        subgraph Observability["Observability Stack"]
            Loki[Loki<br/>log storage]
            Prometheus[Prometheus<br/>metrics]
            Grafana[Grafana<br/>visualization]
        end
    end
    
    %% Application logs flow
    Keycloak -->|stdout/stderr| FluentdDriver
    Postgres -->|stdout/stderr| FluentdDriver
    Nginx -->|stdout/stderr| FluentdDriver
    ProductService -->|stdout/stderr| FluentdDriver
    OrderService -->|stdout/stderr| FluentdDriver
    Frontend -->|stdout/stderr| FluentdDriver
    
    FluentdDriver -->|TCP :24224| FluentBit
    FluentBit -->|HTTP :3100| Loki
    
    %% Tracing flow
    Keycloak -->|OTLP traces| Jaeger
    ProductService -->|OTLP traces| Jaeger
    OrderService -->|OTLP traces| Jaeger
    
    %% Metrics flow
    FluentBit -->|/metrics :2020| Prometheus
    ProductService -->|/metrics| Prometheus
    OrderService -->|/metrics| Prometheus
    
    %% Visualization
    Loki --> Grafana
    Prometheus --> Grafana
    Jaeger --> Grafana
    
    %% Styling
    classDef service fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    classDef logging fill:#fff4e1,stroke:#f57c00,stroke-width:2px
    classDef observability fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    
    class Keycloak,Postgres,Nginx,Jaeger,ProductService,OrderService,Frontend service
    class FluentdDriver,FluentBit logging
    class Loki,Prometheus,Grafana observability
```

## Компоненты системы
1. Fluent Bit (Log Collector)
	Не требует root
	Роль: Принимает логи от Docker logging driver
2. Docker Fluentd Logging Driver
	Встроенный драйвер Docker, который перенаправляет `stdout`/`stderr` контейнеров на внешний Fluentd
3. Loki
	Роль: Хранилище логов
4. Grafana
	Роль: dashboard
5. Prometheus
	Роль: Хранилище метрик
6. Jaeger
	Роль: трейсинг

# Интеграция в сервисы
## Сторонние сервисы (без OTEL)
**Сервисы**: PostgreSQL, Redis, MinIO, Nginx, Jaeger, Kafka UI, pgAdmin
**Решение**: Docker Fluentd logging driver - Fluent Bit
- Пишут логи в stdout/stderr
- Docker сам перенаправляет логи в Fluent Bit
## с OTEL
**Сервисы**: Keycloak
- **OTLP** для трейсинга - Jaeger
- **Fluentd driver** для application-логов - Fluent Bit - Loki
## Init-контейнеры
**Сервисы:** `keycloak-init`, `grafana-init`
- Init-контейнеры выполняются один раз и завершаются
- Логи важны для отладки
- Пишут без OTEL 
## .NET
**Сервисы**: бэкенд
- Можно вркчную настроить AddOpenTelemetry().AddOtlpExporter()
- либо писать напрямую без OTEL

