# Backend build (multi-stage), запускать из корня репозитория.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx ./
COPY src/ src/
RUN dotnet restore src/Barberos.Api/Barberos.Api.csproj
RUN dotnet publish src/Barberos.Api/Barberos.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Barberos.Api.dll"]
