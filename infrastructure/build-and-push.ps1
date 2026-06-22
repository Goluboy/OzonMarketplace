# build-and-push.ps1
$DOCKER_USER = "goluboy" # <-- ОБЯЗАТЕЛЬНО ЗАМЕНИТЕ НА СВОЙ ЛОГИН!
$TAG = "latest"

Write-Host "Logging into Docker Hub..." -ForegroundColor Cyan
docker login
if ($LASTEXITCODE -ne 0) { Write-Host "Docker login failed!" -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "Building and pushing INFRASTRUCTURE images..." -ForegroundColor Yellow

# Nginx
Write-Host "-> marketplace-nginx" -ForegroundColor Gray
docker build -t "$DOCKER_USER/marketplace-nginx:$TAG" ./nginx
docker push "$DOCKER_USER/marketplace-nginx:$TAG"

# Keycloak
Write-Host "-> marketplace-keycloak" -ForegroundColor Gray
docker build -t "$DOCKER_USER/marketplace-keycloak:$TAG" ./keycloak
docker push "$DOCKER_USER/marketplace-keycloak:$TAG"

# Prometheus
Write-Host "-> marketplace-prometheus" -ForegroundColor Gray
docker build -t "$DOCKER_USER/marketplace-prometheus:$TAG" ./monitoring/prometheus
docker push "$DOCKER_USER/marketplace-prometheus:$TAG"

# Loki
Write-Host "-> marketplace-loki" -ForegroundColor Gray
docker build -t "$DOCKER_USER/marketplace-loki:$TAG" ./monitoring/loki
docker push "$DOCKER_USER/marketplace-loki:$TAG"

# Grafana
Write-Host "-> marketplace-grafana" -ForegroundColor Gray
docker build -t "$DOCKER_USER/marketplace-grafana:$TAG" ./monitoring/grafana
docker push "$DOCKER_USER/marketplace-grafana:$TAG"

# Redis
Write-Host "-> marketplace-redis" -ForegroundColor Gray
docker build -t "$DOCKER_USER/marketplace-redis:$TAG" ./monitoring/redis
docker push "$DOCKER_USER/marketplace-redis:$TAG"

Write-Host ""
Write-Host "ALL IMAGES BUILT AND PUSHED SUCCESSFULLY!" -ForegroundColor Green