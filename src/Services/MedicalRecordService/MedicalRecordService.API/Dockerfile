FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Shared/Vitals.ObjectStorage/Vitals.ObjectStorage.csproj", "Shared/Vitals.ObjectStorage/"]
COPY ["Services/MedicalRecordService/MedicalRecordService.API/MedicalRecordService.API.csproj", "Services/MedicalRecordService/MedicalRecordService.API/"]
COPY ["Services/MedicalRecordService/MedicalRecordService.Application/MedicalRecordService.Application.csproj", "Services/MedicalRecordService/MedicalRecordService.Application/"]
COPY ["Services/MedicalRecordService/MedicalRecordService.Domain/MedicalRecordService.Domain.csproj", "Services/MedicalRecordService/MedicalRecordService.Domain/"]
COPY ["Services/MedicalRecordService/MedicalRecordService.Infrastructure/MedicalRecordService.Infrastructure.csproj", "Services/MedicalRecordService/MedicalRecordService.Infrastructure/"]
RUN dotnet restore "Services/MedicalRecordService/MedicalRecordService.API/MedicalRecordService.API.csproj"

COPY Shared/ Shared/
COPY Services/MedicalRecordService/ Services/MedicalRecordService/

WORKDIR /src/Services/MedicalRecordService/MedicalRecordService.API
RUN dotnet publish "MedicalRecordService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MedicalRecordService.API.dll"]
