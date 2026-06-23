FROM grafana/grafana:11.2.0

# 1. Переключаемся на root, чтобы иметь права на создание папок в /opt
USER root

# 2. Копируем конфиг датасорсов
COPY --chown=grafana:root ./datasources.yaml /etc/grafana/provisioning/datasources/datasources.yaml

# 3. Копируем провайдер дашбордов (указатель)
COPY --chown=grafana:root ./dashboards.yml /etc/grafana/provisioning/dashboards/dashboards.yml

# 4. Создаем отдельную папку для дашбордов и рекурсивно отдаем права пользователю grafana
RUN mkdir -p /opt/grafana/dashboards && chown -R grafana:root /opt/grafana

# 5. Копируем сами JSON-файлы дашбордов
COPY --chown=grafana:root ./*.json /opt/grafana/dashboards/

# 6. Возвращаем пользователя grafana для безопасного запуска контейнера
USER grafana