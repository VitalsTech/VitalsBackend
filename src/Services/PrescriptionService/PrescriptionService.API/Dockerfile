FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PrescriptionService.API/PrescriptionService.API.csproj", "PrescriptionService.API/"]
COPY ["PrescriptionService.Application/PrescriptionService.Application.csproj", "PrescriptionService.Application/"]
COPY ["PrescriptionService.Domain/PrescriptionService.Domain.csproj", "PrescriptionService.Domain/"]
COPY ["PrescriptionService.Infrastructure/PrescriptionService.Infrastructure.csproj", "PrescriptionService.Infrastructure/"]
RUN dotnet restore "PrescriptionService.API/PrescriptionService.API.csproj"
COPY . .
WORKDIR "/src/PrescriptionService.API"
RUN dotnet build "PrescriptionService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PrescriptionService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PrescriptionService.API.dll"]
