FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Services/ApiGateway/ApiGateway.API/ApiGateway.API.csproj", "Services/ApiGateway/ApiGateway.API/"]
COPY ["Services/ApiGateway/ApiGateway.Application/ApiGateway.Application.csproj", "Services/ApiGateway/ApiGateway.Application/"]
COPY ["Services/ApiGateway/ApiGateway.Infrastructure/ApiGateway.Infrastructure.csproj", "Services/ApiGateway/ApiGateway.Infrastructure/"]
RUN dotnet restore "Services/ApiGateway/ApiGateway.API/ApiGateway.API.csproj"

COPY Services/ApiGateway/ Services/ApiGateway/

WORKDIR /src/Services/ApiGateway/ApiGateway.API
RUN dotnet publish "ApiGateway.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.API.dll"]
