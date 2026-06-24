FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Services/AITriageService/AITriageService.API/AITriageService.API.csproj", "Services/AITriageService/AITriageService.API/"]
COPY ["Services/AITriageService/AITriageService.Application/AITriageService.Application.csproj", "Services/AITriageService/AITriageService.Application/"]
COPY ["Services/AITriageService/AITriageService.Domain/AITriageService.Domain.csproj", "Services/AITriageService/AITriageService.Domain/"]
COPY ["Services/AITriageService/AITriageService.Infrastructure/AITriageService.Infrastructure.csproj", "Services/AITriageService/AITriageService.Infrastructure/"]
RUN dotnet restore "Services/AITriageService/AITriageService.API/AITriageService.API.csproj"

COPY Shared/ Shared/
COPY Services/AITriageService/ Services/AITriageService/

WORKDIR /src/Services/AITriageService/AITriageService.API
RUN dotnet publish "AITriageService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AITriageService.API.dll"]
