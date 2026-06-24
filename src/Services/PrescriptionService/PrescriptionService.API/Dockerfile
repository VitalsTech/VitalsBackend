FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Shared/Vitals.ESignature/Vitals.ESignature.csproj", "Shared/Vitals.ESignature/"]
COPY ["Services/PrescriptionService/PrescriptionService.API/PrescriptionService.API.csproj", "Services/PrescriptionService/PrescriptionService.API/"]
COPY ["Services/PrescriptionService/PrescriptionService.Application/PrescriptionService.Application.csproj", "Services/PrescriptionService/PrescriptionService.Application/"]
COPY ["Services/PrescriptionService/PrescriptionService.Domain/PrescriptionService.Domain.csproj", "Services/PrescriptionService/PrescriptionService.Domain/"]
COPY ["Services/PrescriptionService/PrescriptionService.Infrastructure/PrescriptionService.Infrastructure.csproj", "Services/PrescriptionService/PrescriptionService.Infrastructure/"]
RUN dotnet restore "Services/PrescriptionService/PrescriptionService.API/PrescriptionService.API.csproj"

COPY Shared/ Shared/
COPY Services/PrescriptionService/ Services/PrescriptionService/

WORKDIR /src/Services/PrescriptionService/PrescriptionService.API
RUN dotnet publish "PrescriptionService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PrescriptionService.API.dll"]
