FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["ProductService/ProductService.Presentation/ProductService.Presentation.csproj", "ProductService/ProductService.Presentation/"]
COPY ["ProductService/ProductService.Application/ProductService.Application.csproj", "ProductService/ProductService.Application/"]
COPY ["ProductService/ProductService.Infrastructure/ProductService.Infrastructure.csproj", "ProductService/ProductService.Infrastructure/"]
COPY ["ProductService/ProductService.Infrastructure.Abstractions/ProductService.Infrastructure.Abstractions.csproj", "ProductService/ProductService.Infrastructure.Abstractions/"]
COPY ["ProductService/ProductService.Domain/ProductService.Domain.csproj", "ProductService/ProductService.Domain/"]
COPY ["S3/S3.csproj", "S3/"]
COPY ["Redis/Redis.csproj", "Redis/"]

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "ProductService/ProductService.Presentation/ProductService.Presentation.csproj"

COPY . .

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "ProductService/ProductService.Presentation/ProductService.Presentation.csproj" \
        -c Release \
        -o /app/publish \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN apk add --no-cache \
        tzdata \
        icu-libs \
        curl \
    && cp /usr/share/zoneinfo/Europe/Moscow /etc/localtime \
    && echo "Europe/Moscow" > /etc/timezone \
    && rm -rf /var/cache/apk/*

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ProductService.Presentation.dll"]