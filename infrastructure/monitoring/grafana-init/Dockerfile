FROM alpine:3.19

# Устанавливаем необходимые утилиты
RUN apk add --no-cache \
    bash \
    curl \
    jq \
    && rm -rf /var/cache/apk/*

# Копируем скрипт и дашборды
COPY init-dashboards.sh /tmp/init-dashboards.sh
COPY dashboards/ /tmp/dashboards/

RUN chmod +x /tmp/init-dashboards.sh

ENTRYPOINT ["/tmp/init-dashboards.sh"]