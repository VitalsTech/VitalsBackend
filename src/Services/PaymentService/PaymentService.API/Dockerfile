FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared/Vitals.Messaging/Vitals.Messaging.csproj", "Shared/Vitals.Messaging/"]
COPY ["Shared/Vitals.AspNetCore.Authentication/Vitals.AspNetCore.Authentication.csproj", "Shared/Vitals.AspNetCore.Authentication/"]
COPY ["Services/PaymentService/PaymentService.API/PaymentService.API.csproj", "Services/PaymentService/PaymentService.API/"]
COPY ["Services/PaymentService/PaymentService.Application/PaymentService.Application.csproj", "Services/PaymentService/PaymentService.Application/"]
COPY ["Services/PaymentService/PaymentService.Domain/PaymentService.Domain.csproj", "Services/PaymentService/PaymentService.Domain/"]
COPY ["Services/PaymentService/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj", "Services/PaymentService/PaymentService.Infrastructure/"]
RUN dotnet restore "Services/PaymentService/PaymentService.API/PaymentService.API.csproj"

COPY Shared/ Shared/
COPY Services/PaymentService/ Services/PaymentService/

WORKDIR /src/Services/PaymentService/PaymentService.API
RUN dotnet publish "PaymentService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.API.dll"]
