# 1. Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# 2. SDK build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["trackrv2-web-api/trackrv2-web-api.csproj", "trackrv2-web-api/"]
COPY ["trackrv2-efc/trackrv2-efc.csproj", "trackrv2-efc/"]

RUN dotnet restore "trackrv2-web-api/trackrv2-web-api.csproj"

COPY . .

WORKDIR "/src/trackrv2-web-api"
RUN dotnet publish "trackrv2-web-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Final runtime image setup
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "trackrv2-web-api.dll"]