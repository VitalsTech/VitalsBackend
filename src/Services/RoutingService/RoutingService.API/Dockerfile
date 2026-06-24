FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Services/RoutingService/RoutingService.API/RoutingService.API.csproj", "Services/RoutingService/RoutingService.API/"]
COPY ["Services/RoutingService/RoutingService.Application/RoutingService.Application.csproj", "Services/RoutingService/RoutingService.Application/"]
COPY ["Services/RoutingService/RoutingService.Domain/RoutingService.Domain.csproj", "Services/RoutingService/RoutingService.Domain/"]
COPY ["Services/RoutingService/RoutingService.Infrastructure/RoutingService.Infrastructure.csproj", "Services/RoutingService/RoutingService.Infrastructure/"]
RUN dotnet restore "Services/RoutingService/RoutingService.API/RoutingService.API.csproj"

COPY Shared/ Shared/
COPY Services/RoutingService/ Services/RoutingService/

WORKDIR /src/Services/RoutingService/RoutingService.API
RUN dotnet publish "RoutingService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RoutingService.API.dll"]
