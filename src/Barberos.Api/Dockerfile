# Backend build (multi-stage), запускать из корня репозитория.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только манифесты — лучше кешируется restore.
COPY *.slnx ./
COPY src/Barberos.Api/Barberos.Api.csproj src/Barberos.Api/
COPY src/Barberos.Application/Barberos.Application.csproj src/Barberos.Application/
COPY src/Barberos.Domain/Barberos.Domain.csproj src/Barberos.Domain/
COPY src/Barberos.Infrastructure/Barberos.Infrastructure.csproj src/Barberos.Infrastructure/
RUN dotnet restore src/Barberos.Api/Barberos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Barberos.Api/Barberos.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Непривилегированный пользователь (образы .NET уже содержат UID 1654 "app").
USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Barberos.Api.dll"]
