FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ApiGateway.API/ApiGateway.API.csproj", "ApiGateway.API/"]
COPY ["ApiGateway.Application/ApiGateway.Application.csproj", "ApiGateway.Application/"]
COPY ["ApiGateway.Infrastructure/ApiGateway.Infrastructure.csproj", "ApiGateway.Infrastructure/"]
RUN dotnet restore "ApiGateway.API/ApiGateway.API.csproj"
COPY . .
WORKDIR "/src/ApiGateway.API"
RUN dotnet publish "ApiGateway.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.API.dll"]
