FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["ProductService/ProductService.Presentation/ProductService.Presentation.csproj", "ProductService.Presentation/"]
COPY ["ProductService/ProductService.Application/ProductService.Application.csproj", "ProductService.Application/"]
COPY ["ProductService/ProductService.Infrastructure/ProductService.Infrastructure.csproj", "ProductService.Infrastructure/"]
COPY ["ProductService/ProductService.Infrastructure.Abstractions/ProductService.Infrastructure.Abstractions.csproj", "ProductService.Infrastructure.Abstractions/"]
COPY ["ProductService/ProductService.Domain/ProductService.Domain.csproj", "ProductService.Domain/"]
COPY ["S3/S3.csproj", "S3/"]

RUN dotnet restore "ProductService.Presentation/ProductService.Presentation.csproj"

COPY . .
WORKDIR /src/ProductService/ProductService.Presentation
RUN dotnet publish "ProductService.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "ProductService.Presentation.dll"]