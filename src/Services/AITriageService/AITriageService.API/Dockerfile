FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AITriageService.API/AITriageService.API.csproj", "AITriageService.API/"]
COPY ["AITriageService.Application/AITriageService.Application.csproj", "AITriageService.Application/"]
COPY ["AITriageService.Domain/AITriageService.Domain.csproj", "AITriageService.Domain/"]
COPY ["AITriageService.Infrastructure/AITriageService.Infrastructure.csproj", "AITriageService.Infrastructure/"]
RUN dotnet restore "AITriageService.API/AITriageService.API.csproj"
COPY . .
WORKDIR "/src/AITriageService.API"
RUN dotnet publish "AITriageService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AITriageService.API.dll"]
