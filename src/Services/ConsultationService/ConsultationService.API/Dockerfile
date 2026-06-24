FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ConsultationService.API/ConsultationService.API.csproj", "ConsultationService.API/"]
COPY ["ConsultationService.Application/ConsultationService.Application.csproj", "ConsultationService.Application/"]
COPY ["ConsultationService.Domain/ConsultationService.Domain.csproj", "ConsultationService.Domain/"]
COPY ["ConsultationService.Infrastructure/ConsultationService.Infrastructure.csproj", "ConsultationService.Infrastructure/"]
RUN dotnet restore "ConsultationService.API/ConsultationService.API.csproj"
COPY . .
WORKDIR "/src/ConsultationService.API"
RUN dotnet build "ConsultationService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsultationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsultationService.API.dll"]
