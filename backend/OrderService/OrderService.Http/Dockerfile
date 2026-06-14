# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OrderService.Http/OrderService.Http.csproj", "OrderService.Http/"]
COPY ["OrderService.Infrastructure.EventBus/OrderService.Infrastructure.EventBus.csproj", "OrderService.Infrastructure.EventBus/"]
COPY ["OrderService.Domain/OrderService.Domain.csproj", "OrderService.Domain/"]
COPY ["OrderService.Infrastructure.Persistence/OrderService.Infrastructure.Persistence.csproj", "OrderService.Infrastructure.Persistence/"]
COPY ["OrderService.UseCases.Commands/OrderService.UseCases.Commands.csproj", "OrderService.UseCases.Commands/"]
COPY ["OrderService.UseCases.Queries/OrderService.UseCases.Queries.csproj", "OrderService.UseCases.Queries/"]
RUN dotnet restore "./OrderService.Http/OrderService.Http.csproj"
COPY . .
WORKDIR "/src/OrderService.Http"
RUN dotnet build "./OrderService.Http.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrderService.Http.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.Http.dll"]