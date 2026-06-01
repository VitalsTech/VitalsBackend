FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MedicalRecordService.API/MedicalRecordService.API.csproj", "MedicalRecordService.API/"]
COPY ["MedicalRecordService.Application/MedicalRecordService.Application.csproj", "MedicalRecordService.Application/"]
COPY ["MedicalRecordService.Domain/MedicalRecordService.Domain.csproj", "MedicalRecordService.Domain/"]
COPY ["MedicalRecordService.Infrastructure/MedicalRecordService.Infrastructure.csproj", "MedicalRecordService.Infrastructure/"]
RUN dotnet restore "MedicalRecordService.API/MedicalRecordService.API.csproj"
COPY . .
WORKDIR "/src/MedicalRecordService.API"
RUN dotnet publish "MedicalRecordService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MedicalRecordService.API.dll"]
