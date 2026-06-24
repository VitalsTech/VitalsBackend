FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Shared/Vitals.ObjectStorage/Vitals.ObjectStorage.csproj", "Shared/Vitals.ObjectStorage/"]
COPY ["Services/IntegrationService/IntegrationService.API/IntegrationService.API.csproj", "Services/IntegrationService/IntegrationService.API/"]
COPY ["Services/IntegrationService/IntegrationService.Application/IntegrationService.Application.csproj", "Services/IntegrationService/IntegrationService.Application/"]
COPY ["Services/IntegrationService/IntegrationService.Infrastructure/IntegrationService.Infrastructure.csproj", "Services/IntegrationService/IntegrationService.Infrastructure/"]
RUN dotnet restore "Services/IntegrationService/IntegrationService.API/IntegrationService.API.csproj"

COPY Shared/ Shared/
COPY Services/IntegrationService/ Services/IntegrationService/

WORKDIR /src/Services/IntegrationService/IntegrationService.API
RUN dotnet publish "IntegrationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IntegrationService.API.dll"]
