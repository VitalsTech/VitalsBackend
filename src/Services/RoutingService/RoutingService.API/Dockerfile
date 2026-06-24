FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RoutingService.API/RoutingService.API.csproj", "RoutingService.API/"]
COPY ["RoutingService.Application/RoutingService.Application.csproj", "RoutingService.Application/"]
COPY ["RoutingService.Domain/RoutingService.Domain.csproj", "RoutingService.Domain/"]
COPY ["RoutingService.Infrastructure/RoutingService.Infrastructure.csproj", "RoutingService.Infrastructure/"]
RUN dotnet restore "RoutingService.API/RoutingService.API.csproj"
COPY . .
WORKDIR "/src/RoutingService.API"
RUN dotnet build "RoutingService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RoutingService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RoutingService.API.dll"]
