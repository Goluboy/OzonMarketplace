FROM grafana/grafana:latest

# Копируем datasource'ы в правильную директорию
COPY datasources/ /etc/grafana/provisioning/datasources/

# Копируем дашборды И их провайдер
COPY dashboards/ /var/lib/grafana/dashboards/
COPY dashboards/dashboard-provider.yaml /etc/grafana/provisioning/dashboards/

# Устанавливаем плагины (опционально, если нужны)
# RUN grafana-cli plugins install grafana-piechart-panel