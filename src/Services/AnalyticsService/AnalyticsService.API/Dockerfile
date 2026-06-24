FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Services/AnalyticsService/AnalyticsService.API/AnalyticsService.API.csproj", "Services/AnalyticsService/AnalyticsService.API/"]
COPY ["Services/AnalyticsService/AnalyticsService.Application/AnalyticsService.Application.csproj", "Services/AnalyticsService/AnalyticsService.Application/"]
COPY ["Services/AnalyticsService/AnalyticsService.Domain/AnalyticsService.Domain.csproj", "Services/AnalyticsService/AnalyticsService.Domain/"]
COPY ["Services/AnalyticsService/AnalyticsService.Infrastructure/AnalyticsService.Infrastructure.csproj", "Services/AnalyticsService/AnalyticsService.Infrastructure/"]
RUN dotnet restore "Services/AnalyticsService/AnalyticsService.API/AnalyticsService.API.csproj"

COPY Shared/ Shared/
COPY Services/AnalyticsService/ Services/AnalyticsService/

WORKDIR /src/Services/AnalyticsService/AnalyticsService.API
RUN dotnet publish "AnalyticsService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AnalyticsService.API.dll"]
