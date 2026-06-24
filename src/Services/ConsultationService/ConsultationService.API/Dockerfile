FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Shared/Vitals.ESignature/Vitals.ESignature.csproj", "Shared/Vitals.ESignature/"]
COPY ["Services/ConsultationService/ConsultationService.API/ConsultationService.API.csproj", "Services/ConsultationService/ConsultationService.API/"]
COPY ["Services/ConsultationService/ConsultationService.Application/ConsultationService.Application.csproj", "Services/ConsultationService/ConsultationService.Application/"]
COPY ["Services/ConsultationService/ConsultationService.Domain/ConsultationService.Domain.csproj", "Services/ConsultationService/ConsultationService.Domain/"]
COPY ["Services/ConsultationService/ConsultationService.Infrastructure/ConsultationService.Infrastructure.csproj", "Services/ConsultationService/ConsultationService.Infrastructure/"]
RUN dotnet restore "Services/ConsultationService/ConsultationService.API/ConsultationService.API.csproj"

COPY Shared/ Shared/
COPY Services/ConsultationService/ Services/ConsultationService/

WORKDIR /src/Services/ConsultationService/ConsultationService.API
RUN dotnet publish "ConsultationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConsultationService.API.dll"]
