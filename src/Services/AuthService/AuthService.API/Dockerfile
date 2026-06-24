FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Services/AuthService/AuthService.API/AuthService.API.csproj", "Services/AuthService/AuthService.API/"]
COPY ["Services/AuthService/AuthService.Application/AuthService.Application.csproj", "Services/AuthService/AuthService.Application/"]
COPY ["Services/AuthService/AuthService.Domain/AuthService.Domain.csproj", "Services/AuthService/AuthService.Domain/"]
COPY ["Services/AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "Services/AuthService/AuthService.Infrastructure/"]
RUN dotnet restore "Services/AuthService/AuthService.API/AuthService.API.csproj"

COPY Shared/ Shared/
COPY Services/AuthService/ Services/AuthService/

WORKDIR /src/Services/AuthService/AuthService.API
RUN dotnet publish "AuthService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuthService.API.dll"]
