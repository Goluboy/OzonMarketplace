#!/bin/bash
set -euo pipefail

# === Конфигурация ожидания ===
MAX_WAIT_SECONDS=180   # Ждать максимум 3 минуты
RETRY_INTERVAL=3       # Проверять каждые 3 секунды

echo "[INFO] Waiting for Keycloak HTTP to be available..."
elapsed=0
until (echo > /dev/tcp/keycloak/8080) 2>/dev/null; do
  sleep "$RETRY_INTERVAL"
  elapsed=$((elapsed + RETRY_INTERVAL))
  if [ "$elapsed" -ge "$MAX_WAIT_SECONDS" ]; then
    echo "[ERROR] Keycloak HTTP not available after ${MAX_WAIT_SECONDS}s"
    exit 1
  fi
done
echo "[OK] Keycloak HTTP available after ${elapsed}s"

# === Аутентификация в Keycloak ===
echo "[INFO] Authenticating to Keycloak admin API..."
elapsed=0
until /opt/keycloak/bin/kcadm.sh config credentials \
  --server "${KC_SERVER_URL}" \
  --realm master \
  --user "${KC_ADMIN}" \
  --password "${KC_ADMIN_PASSWORD}" 2>/dev/null; do
  sleep "$RETRY_INTERVAL"
  elapsed=$((elapsed + RETRY_INTERVAL))
  if [ "$elapsed" -ge "$MAX_WAIT_SECONDS" ]; then
    echo "[ERROR] Cannot authenticate to Keycloak after ${MAX_WAIT_SECONDS}s"
    exit 1
  fi
done
echo "[OK] Authenticated after ${elapsed}s"

# === Ожидание импорта realm ===
echo "[INFO] Waiting for realm '${KC_REALM}' to be available..."
elapsed=0
while [ "$elapsed" -lt "$MAX_WAIT_SECONDS" ]; do
  if /opt/keycloak/bin/kcadm.sh get "realms/${KC_REALM}" >/dev/null 2>&1; then
    echo "[OK] Realm '${KC_REALM}' is available after ${elapsed}s"
    break
  fi
  
  sleep "$RETRY_INTERVAL"
  elapsed=$((elapsed + RETRY_INTERVAL))
  echo "[WAIT] Realm not ready yet (${elapsed}s / ${MAX_WAIT_SECONDS}s)"
done

# === Fallback: если realm так и не появился, создаём вручную ===
if ! /opt/keycloak/bin/kcadm.sh get "realms/${KC_REALM}" >/dev/null 2>&1; then
  echo "[WARN] Realm '${KC_REALM}' was not imported automatically"
  echo "[WARN] Creating realm manually via kcadm..."
  
  /opt/keycloak/bin/kcadm.sh create realms \
    -s "realm=${KC_REALM}" \
    -s "enabled=true" \
    -s "registrationAllowed=false"
  
  echo "[OK] Realm '${KC_REALM}' created manually"
  
  # Создаём clients
  for CLIENT_ID in marketplace-app admin-panel; do
    /opt/keycloak/bin/kcadm.sh create clients -r "${KC_REALM}" \
      -s "clientId=${CLIENT_ID}" \
      -s "enabled=true" \
      -s "publicClient=true" \
      -s "directAccessGrantsEnabled=true" \
      -s "standardFlowEnabled=true"
    echo "[OK] Client '${CLIENT_ID}' created"
  done
fi

# === Создание ролей ===
echo "[INFO] Ensuring roles exist..."
for ROLE in customer admin seller; do
  if ! /opt/keycloak/bin/kcadm.sh get roles -r "${KC_REALM}" --fields name --format csv 2>/dev/null | grep -q "\"${ROLE}\""; then
    /opt/keycloak/bin/kcadm.sh create roles -r "${KC_REALM}" -s "name=${ROLE}"
    echo "[OK] Created role: ${ROLE}"
  else
    echo "[SKIP] Role already exists: ${ROLE}"
  fi
done

# === Функция создания пользователя ===
create_user() {
  local EMAIL=$1
  local PASSWORD=$2
  local ROLE=$3
  
  echo "[INFO] Processing user: ${EMAIL} (role: ${ROLE})"
  
  EXISTING=$(/opt/keycloak/bin/kcadm.sh get users -r "${KC_REALM}" \
    -q "username=${EMAIL}" --fields id --format csv 2>/dev/null | tr -d '"' | head -n 1 || true)
  
  if [ -z "$EXISTING" ]; then
    # Создаём пользователя с заполненным профилем (чтобы не было UPDATE_PROFILE)
    /opt/keycloak/bin/kcadm.sh create users -r "${KC_REALM}" \
      -s "username=${EMAIL}" \
      -s "email=${EMAIL}" \
      -s "emailVerified=true" \
      -s "enabled=true" \
      -s "firstName=${EMAIL%%@*}" \
      -s "lastName=User"
    
    USER_ID=$(/opt/keycloak/bin/kcadm.sh get users -r "${KC_REALM}" \
      -q "username=${EMAIL}" --fields id --format csv | tr -d '"' | head -n 1)
    echo "[OK] User created with ID: ${USER_ID}"
  else
    USER_ID=$EXISTING
    echo "[SKIP] User already exists (ID: ${USER_ID})"
  fi
  
  # Устанавливаем пароль
  /opt/keycloak/bin/kcadm.sh update "users/${USER_ID}/reset-password" -r "${KC_REALM}" \
    -s "type=password" \
    -s "value=${PASSWORD}" \
    -s "temporary=false" -n
  
  # Удаляем все required actions (чтобы не было "Account is not fully set up")
  /opt/keycloak/bin/kcadm.sh update "users/${USER_ID}" -r "${KC_REALM}" \
    -s "requiredActions=[]" -n
  
  # Назначаем роль
  if ! /opt/keycloak/bin/kcadm.sh get "users/${USER_ID}/role-mappings/realm" -r "${KC_REALM}" \
    --fields name --format csv 2>/dev/null | grep -q "\"${ROLE}\""; then
    /opt/keycloak/bin/kcadm.sh add-roles -r "${KC_REALM}" \
      --uusername "${EMAIL}" --rolename "${ROLE}"
    echo "[OK] Role '${ROLE}' assigned"
  else
    echo "[SKIP] Role '${ROLE}' already assigned"
  fi
}

# === Создание тестовых пользователей ===
create_user "${TEST_USER_EMAIL}" "${TEST_USER_PASSWORD}" "customer"
create_user "${ADMIN_USER_EMAIL}" "${ADMIN_USER_PASSWORD}" "admin"


echo ""
echo "[DONE] Keycloak initialization complete!"
echo "   Customer: ${TEST_USER_EMAIL} / ${TEST_USER_PASSWORD}"
echo "   Admin:    ${ADMIN_USER_EMAIL} / ${ADMIN_USER_PASSWORD}"