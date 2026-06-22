FROM quay.io/keycloak/keycloak:26.0

# Копируем realm (владелец не важен, так как это просто данные)
COPY realm-export.json /opt/keycloak/data/import/marketplace-realm.json

# Копируем скрипт и СРАЗУ даем ему права на выполнение (755)
# Это обходит проблему с правами пользователя keycloak
COPY --chmod=755 init-users.sh /tmp/init-users.sh