#!/bin/bash
set -euo pipefail

# ============================================
# Конфигурация
# ============================================
GRAFANA_URL="${GRAFANA_URL}"
GRAFANA_USER="${GRAFANA_ADMIN_USER:-admin}"
GRAFANA_PASSWORD="${GRAFANA_ADMIN_PASSWORD:-admin}"
DASHBOARDS_DIR="/tmp/dashboards"
FOLDER_UID="marketplace"
FOLDER_TITLE="Marketplace"
MAX_RETRIES=30
RETRY_INTERVAL=3

# ============================================
# Функция ожидания готовности Grafana
# ============================================
wait_for_grafana() {
    echo "[INFO] Waiting for Grafana at ${GRAFANA_URL}..."
    
    local retries=0
    local http_code
    
    # Пробуем оба возможных пути
    local paths=("/api/health" "/monitoring/api/health")
    
    while [ "$retries" -lt "$MAX_RETRIES" ]; do
        for path in "${paths[@]}"; do
            http_code=$(curl -s -o /dev/null -w "%{http_code}" \
                -m 5 --connect-timeout 3 \
                "${GRAFANA_URL}${path}" 2>/dev/null || echo "000")
            
            if [ "$http_code" = "200" ]; then
                echo "[OK] Grafana is ready! (path: ${path})"
                # Запоминаем правильный путь для дальнейших запросов
                export GRAFANA_HEALTH_PATH="$path"
                return 0
            fi
            
            echo "[WAIT] Attempt $((retries + 1))/${MAX_RETRIES} - path ${path} returned HTTP ${http_code}"
        done
        
        retries=$((retries + 1))
        sleep "$RETRY_INTERVAL"
    done
    
    echo "[ERROR] Grafana did not become ready on any path"
    exit 1
}

# ============================================
# Функция создания папки
# ============================================
ensure_folder() {
    echo "[INFO] Ensuring folder '${FOLDER_TITLE}' exists..."
    
    # Проверяем существование папки
    local response
    response=$(curl -sf -u "${GRAFANA_USER}:${GRAFANA_PASSWORD}" \
        "${GRAFANA_URL}/api/folders/${FOLDER_UID}" 2>/dev/null || echo "")
    
    if [ -n "$response" ]; then
        echo "[OK] Folder already exists"
        return 0
    fi
    
    # Создаём папку
    echo "[INFO] Creating folder..."
    curl -sf -X POST \
        -u "${GRAFANA_USER}:${GRAFANA_PASSWORD}" \
        -H "Content-Type: application/json" \
        -d "{\"title\":\"${FOLDER_TITLE}\",\"uid\":\"${FOLDER_UID}\"}" \
        "${GRAFANA_URL}/api/folders" > /dev/null
    
    echo "[OK] Folder created"
}

# ============================================
# Определение формата дашборда
# ============================================
detect_dashboard_format() {
    local file=$1
    
    # Проверяем наличие поля "kind" (v2 формат)
    local kind
    kind=$(jq -r '.kind // empty' "$file" 2>/dev/null || echo "")
    
    if [ "$kind" = "Dashboard" ]; then
        echo "v2"
        return
    fi
    
    # Проверяем legacy формат
    local has_dashboard
    has_dashboard=$(jq -r 'has("dashboard")' "$file" 2>/dev/null || echo "false")
    
    if [ "$has_dashboard" = "true" ]; then
        echo "legacy"
        return
    fi
    
    # Неизвестный формат
    echo "unknown"
}

# ============================================
# Импорт v2 дашборда через новый API
# ============================================
import_dashboard_v2() {
    local file=$1
    local filename
    filename=$(basename "$file")
    
    echo "[INFO] Importing ${filename} via v2 API..."
    
    # Конвертируем CRLF в LF
    local dashboard_json
    dashboard_json=$(tr -d '\r' < "$file")
    
    # Извлекаем name из metadata
    local dash_name
    dash_name=$(echo "$dashboard_json" | jq -r '.metadata.name // "dashboard-'$(date +%s)'"')
    
    # Добавляем folderUid в metadata.labels если его нет
    dashboard_json=$(echo "$dashboard_json" | jq --arg folderUid "$FOLDER_UID" '
        .metadata.labels = (.metadata.labels // {}) + {"folderUid": $folderUid}
    ')
    
    # Пробуем v2 API endpoint (Grafana 11+)
    local http_code
    local response
    
    response=$(curl -sf -w "\n%{http_code}" -X PUT \
        -u "${GRAFANA_USER}:${GRAFANA_PASSWORD}" \
        -H "Content-Type: application/json" \
        -d "$dashboard_json" \
        "${GRAFANA_URL}/apis/dashboard.grafana.app/v2/namespaces/default/dashboards/${dash_name}" 2>/dev/null || echo -e "\n000")
    
    http_code=$(echo "$response" | tail -1)
    
    if [ "$http_code" = "200" ] || [ "$http_code" = "201" ]; then
        echo "[OK] ${filename} imported via v2 API"
        return 0
    fi
    
    echo "[WARN] v2 API returned HTTP ${http_code}, will try conversion to legacy"
    return 1
}

# ============================================
# Конвертация v2 в legacy формат
# ============================================
convert_v2_to_legacy() {
    local file=$1
    
    # Конвертируем CRLF в LF на лету (решает проблему Windows)
    local clean_json
    clean_json=$(tr -d '\r' < "$file")
    
    # Конвертируем через jq с правильным синтаксисом
    echo "$clean_json" | jq --arg folderUid "$FOLDER_UID" '{
        dashboard: {
            uid: (.metadata.uid // .metadata.name // "dashboard"),
            title: .spec.title,
            panels: (.spec.panels // []),
            templating: (.spec.templating // {}),
            annotations: (.spec.annotations // {}),
            schemaVersion: (.spec.schemaVersion // 39),
            version: (.metadata.generation // 1),
            tags: (.metadata.labels.tags // []),
            time: (.spec.time // {}),
            timezone: (.spec.timezone // "browser"),
            editable: true,
            graphTooltip: (.spec.graphTooltip // 0)
        },
        overwrite: true,
        folderUid: $folderUid
    }'
}

# ============================================
# Импорт legacy дашборда
# ============================================
import_dashboard_legacy() {
    local file=$1
    local format=$2  # "legacy" или "converted"
    local filename
    filename=$(basename "$file")
    
    local dashboard_json
    
    if [ "$format" = "legacy" ]; then
        # Конвертируем CRLF в LF и читаем файл
        dashboard_json=$(tr -d '\r' < "$file")
        
        # Убедимся, что есть overwrite и folderUid
        dashboard_json=$(echo "$dashboard_json" | jq --arg folderUid "$FOLDER_UID" '
            . + {overwrite: true, folderUid: $folderUid}
        ')
    else
        # Конвертируем из v2
        dashboard_json=$(convert_v2_to_legacy "$file")
    fi
    
    echo "[INFO] Importing ${filename} via legacy API..."
    
    # Проверяем, что JSON валидный
    if ! echo "$dashboard_json" | jq empty 2>/dev/null; then
        echo "[ERROR] ${filename} contains invalid JSON after conversion"
        return 1
    fi
    
    local response
    response=$(curl -sf -w "\n%{http_code}" -X POST \
        -u "${GRAFANA_USER}:${GRAFANA_PASSWORD}" \
        -H "Content-Type: application/json" \
        -d "$dashboard_json" \
        "${GRAFANA_URL}/api/dashboards/db" 2>/dev/null || echo -e "\n000")
    
    local http_code
    http_code=$(echo "$response" | tail -1)
    local body
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" = "200" ]; then
        echo "[OK] ${filename} imported via legacy API"
        return 0
    else
        echo "[ERROR] ${filename} import failed with HTTP ${http_code}"
        echo "       Response: $body"
        return 1
    fi
}

# ============================================
# Основная функция импорта одного дашборда
# ============================================
import_dashboard() {
    local file=$1
    local filename
    filename=$(basename "$file")
    
    echo ""
    echo "============================================"
    echo "Processing: ${filename}"
    echo "============================================"
    
    # Определяем формат
    local format
    format=$(detect_dashboard_format "$file")
    
    case "$format" in
        "v2")
            echo "[INFO] Detected v2 format"
            
            # Сначала пробуем v2 API
            if import_dashboard_v2 "$file"; then
                return 0
            fi
            
            # Fallback: конвертируем в legacy
            echo "[INFO] Falling back to legacy format via conversion..."
            if import_dashboard_legacy "$file" "converted"; then
                return 0
            fi
            
            echo "[ERROR] Failed to import ${filename} with both methods"
            return 1
            ;;
            
        "legacy")
            echo "[INFO] Detected legacy format"
            import_dashboard_legacy "$file" "legacy"
            ;;
            
        *)
            echo "[ERROR] Unknown dashboard format in ${filename}"
            return 1
            ;;
    esac
}

# ============================================
# MAIN
# ============================================
main() {
    echo "╔══════════════════════════════════════════╗"
    echo "║   Grafana Dashboard Importer (v2+legacy) ║"
    echo "╚══════════════════════════════════════════╝"
    echo ""
    
    # Ожидаем готовности Grafana
    wait_for_grafana
    
    # Создаём папку
    ensure_folder
    
    # Проверяем наличие дашбордов
    local count
    count=$(find "$DASHBOARDS_DIR" -name "*.json" | wc -l)
    
    if [ "$count" -eq 0 ]; then
        echo "[WARN] No dashboard JSON files found in ${DASHBOARDS_DIR}"
        exit 0
    fi
    
    echo ""
    echo "[INFO] Found ${count} dashboard(s) to import"
    
    # Импортируем каждый дашборд
    local success=0
    local failed=0
    
    for dashboard in "${DASHBOARDS_DIR}"/*.json; do
        if [ -f "$dashboard" ]; then
            if import_dashboard "$dashboard"; then
                success=$((success + 1))
            else
                failed=$((failed + 1))
            fi
        fi
    done
    
    echo ""
    echo "╔══════════════════════════════════════════╗"
    echo "║   Import Summary                          ║"
    echo "╠══════════════════════════════════════════╣"
    printf "║   ✅ Success: %-26s║\n" "$success"
    printf "║   ❌ Failed:  %-26s║\n" "$failed"
    echo "╚══════════════════════════════════════════╝"
    echo ""
    
    if [ "$failed" -gt 0 ]; then
        echo "[WARN] Some dashboards failed to import"
        exit 1
    fi
    
    echo "[DONE] All dashboards imported successfully!"
    echo "       Open Grafana → Dashboards → Browse → ${FOLDER_TITLE}"
}

main "$@"