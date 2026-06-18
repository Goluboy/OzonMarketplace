# syntax=docker/dockerfile:1.7

# ============================================================
# Stage 1: Base (runtime для production и VS Fast Mode)
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apk add --no-cache \
        tzdata \
        icu-libs \
        curl \
    && cp /usr/share/zoneinfo/Europe/Moscow /etc/localtime \
    && echo "Europe/Moscow" > /etc/timezone \
    && rm -rf /var/cache/apk/*

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:8080

USER $APP_UID

# ============================================================
# Stage 2: Build (SDK с cache mounts)
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["OrderService.Http/OrderService.Http.csproj", "OrderService.Http/"]
COPY ["OrderService.Infrastructure.EventBus/OrderService.Infrastructure.EventBus.csproj", "OrderService.Infrastructure.EventBus/"]
COPY ["OrderService.Domain/OrderService.Domain.csproj", "OrderService.Domain/"]
COPY ["OrderService.Infrastructure.Persistence/OrderService.Infrastructure.Persistence.csproj", "OrderService.Infrastructure.Persistence/"]
COPY ["OrderService.UseCases.Commands/OrderService.UseCases.Commands.csproj", "OrderService.UseCases.Commands/"]
COPY ["OrderService.UseCases.Queries/OrderService.UseCases.Queries.csproj", "OrderService.UseCases.Queries/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "./OrderService.Http/OrderService.Http.csproj"

COPY . .
WORKDIR "/src/OrderService.Http"

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=obj,target=/src/OrderService.Http/obj \
    dotnet build "./OrderService.Http.csproj" \
        -c $BUILD_CONFIGURATION \
        -o /app/build \
        --no-restore

# ============================================================
# Stage 3: Dev (для разработки с hot reload)
# ============================================================
FROM build AS dev
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080
WORKDIR /src/OrderService.Http
CMD ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://+:8080"]

# ============================================================
# Stage 4: Publish
# ============================================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=obj,target=/src/OrderService.Http/obj \
    dotnet publish "./OrderService.Http.csproj" \
        -c $BUILD_CONFIGURATION \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

# ============================================================
# Stage 5: Final (production)
# ============================================================
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "OrderService.Http.dll"]