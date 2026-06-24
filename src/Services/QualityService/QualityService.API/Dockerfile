FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Services/QualityService/QualityService.API/QualityService.API.csproj", "Services/QualityService/QualityService.API/"]
COPY ["Services/QualityService/QualityService.Application/QualityService.Application.csproj", "Services/QualityService/QualityService.Application/"]
COPY ["Services/QualityService/QualityService.Domain/QualityService.Domain.csproj", "Services/QualityService/QualityService.Domain/"]
COPY ["Services/QualityService/QualityService.Infrastructure/QualityService.Infrastructure.csproj", "Services/QualityService/QualityService.Infrastructure/"]
RUN dotnet restore "Services/QualityService/QualityService.API/QualityService.API.csproj"

COPY Shared/ Shared/
COPY Services/QualityService/ Services/QualityService/

WORKDIR /src/Services/QualityService/QualityService.API
RUN dotnet publish "QualityService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "QualityService.API.dll"]
