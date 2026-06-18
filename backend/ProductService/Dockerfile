# syntax=docker/dockerfile:1.7

# ============================================================
# Stage 1: Build (с кэшированием NuGet)
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# 1. Копируем ТОЛЬКО csproj файлы для restore (кэшируется)
COPY ["ProductService/ProductService.Presentation/ProductService.Presentation.csproj", "ProductService/ProductService.Presentation/"]
COPY ["ProductService/ProductService.Application/ProductService.Application.csproj", "ProductService/ProductService.Application/"]
COPY ["ProductService/ProductService.Infrastructure/ProductService.Infrastructure.csproj", "ProductService/ProductService.Infrastructure/"]
COPY ["ProductService/ProductService.Infrastructure.Abstractions/ProductService.Infrastructure.Abstractions.csproj", "ProductService/ProductService.Infrastructure.Abstractions/"]
COPY ["ProductService/ProductService.Domain/ProductService.Domain.csproj", "ProductService/ProductService.Domain/"]
COPY ["S3/S3.csproj", "S3/"]

# 2. Restore с cache mount (NuGet пакеты кэшируются между сборками)
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "ProductService/ProductService.Presentation/ProductService.Presentation.csproj"

# 3. Копируем весь исходный код
COPY . .

# 4. Publish БЕЗ --no-restore (пусть автоматически восстановит все зависимости)
#    Убираем cache mount для obj/ - он не нужен и может вызывать конфликты
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "ProductService/ProductService.Presentation/ProductService.Presentation.csproj" \
        -c Release \
        -o /app/publish \
        /p:UseAppHost=false

# ============================================================
# Stage 2: Dev (для разработки с hot reload)
# ============================================================
FROM build AS dev
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080
WORKDIR /src/ProductService/ProductService.Presentation
CMD ["dotnet", "watch", "run", "--no-launch-profile"]

# ============================================================
# Stage 3: Final (production runtime)
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Timezone и ICU для корректной работы с датами и строками
RUN apk add --no-cache \
        tzdata \
        icu-libs \
        curl \
    && cp /usr/share/zoneinfo/Europe/Moscow /etc/localtime \
    && echo "Europe/Moscow" > /etc/timezone \
    && rm -rf /var/cache/apk/*

# Неинвариантная globalization (для Currency, дат и т.д.)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Копируем только опубликованные файлы
COPY --from=build /app/publish .

# Запуск от непривилегированного пользователя (безопасность)
USER $APP_UID

# Healthcheck для orchestrator'ов
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ProductService.Presentation.dll"]